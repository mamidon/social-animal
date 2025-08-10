using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Infrastructure.Services;

public class InvitationService : ServiceBase, IInvitationService
{
    private readonly IInvitationRepo _invitationRepo;
    private readonly IEventRepo _eventRepo;
    private readonly IReservationRepo _reservationRepo;
    private readonly ICrudRepo _crudRepo;
    
    public InvitationService(
        IInvitationRepo invitationRepo,
        IEventRepo eventRepo,
        IReservationRepo reservationRepo,
        ICrudRepo crudRepo,
        ILoggerPortal logger,
        IClock clock)
        : base(logger, clock)
    {
        _invitationRepo = invitationRepo;
        _eventRepo = eventRepo;
        _reservationRepo = reservationRepo;
        _crudRepo = crudRepo;
    }
    
    public async Task<InvitationRecord> CreateInvitationAsync(InvitationStub stub)
    {
        using var scope = LogMethodEntry(nameof(CreateInvitationAsync), new Dictionary<string, object>
        {
            ["EventId"] = stub.EventId,
            ["Slug"] = stub.Slug
        });
        
        // Validate event exists
        var eventRecord = await _eventRepo.Events.FindByIdAsync(stub.EventId);
        if (eventRecord == null)
        {
            throw new InvalidOperationException($"Event with ID {stub.EventId} not found");
        }
        
        // Generate unique slug within event (use the provided slug as base)
        var uniqueSlug = await EnsureUniqueSlugAsync(stub.Slug, async slug =>
        {
            var existingInvitations = await _invitationRepo.GetByEventIdAsync(stub.EventId);
            return existingInvitations.Any(i => i.Slug == slug);
        });
        
        var now = Clock.GetCurrentInstant();
        var invitationRecord = new InvitationRecord
        {
            Id = 0, // Will be set by database
            EventId = stub.EventId,
            Slug = uniqueSlug,
            CreatedOn = now,
            UpdatedOn = null,
            DeletedAt = null
        };
        
        var result = await _crudRepo.CreateAsync(invitationRecord);
        
        LogOperationComplete("CreateInvitation", new { InvitationId = result.Id, Slug = result.Slug });
        return result;
    }
    
    public async Task<List<InvitationRecord>> CreateBulkInvitationsAsync(long eventId, List<InvitationStub> stubs)
    {
        using var scope = LogMethodEntry(nameof(CreateBulkInvitationsAsync), new Dictionary<string, object>
        {
            ["EventId"] = eventId,
            ["Count"] = stubs.Count
        });
        
        // Validate event exists
        var eventRecord = await _eventRepo.Events.FindByIdAsync(eventId);
        if (eventRecord == null)
        {
            throw new InvalidOperationException($"Event with ID {eventId} not found");
        }
        
        var results = new List<InvitationRecord>();
        
        foreach (var stub in stubs)
        {
            if (stub.EventId != eventId)
            {
                throw new ArgumentException($"All invitation stubs must have EventId {eventId}");
            }
            
            var invitation = await CreateInvitationAsync(stub);
            results.Add(invitation);
        }
        
        LogOperationComplete("CreateBulkInvitations", new { EventId = eventId, Created = results.Count });
        return results;
    }
    
    public async Task<InvitationRecord> UpdateInvitationAsync(long invitationId, InvitationStub stub)
    {
        using var scope = LogMethodEntry(nameof(UpdateInvitationAsync), new Dictionary<string, object>
        {
            ["InvitationId"] = invitationId,
            ["Slug"] = stub.Slug
        });
        
        var existing = await _invitationRepo.Invitations.FindByIdAsync(invitationId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Invitation with ID {invitationId} not found");
        }
        
        // Generate new unique slug if changed
        string newSlug = existing.Slug;
        if (stub.Slug != existing.Slug)
        {
            newSlug = await EnsureUniqueSlugAsync(stub.Slug, async slug =>
            {
                var existingInvitations = await _invitationRepo.GetByEventIdAsync(existing.EventId);
                return existingInvitations.Any(i => i.Slug == slug && i.Id != invitationId);
            });
        }
        
        var now = Clock.GetCurrentInstant();
        var updatedRecord = existing with
        {
            Slug = newSlug,
            UpdatedOn = now
        };
        
        var result = await _crudRepo.UpdateAsync(updatedRecord);
        
        LogOperationComplete("UpdateInvitation", new { InvitationId = result.Id, Slug = result.Slug });
        return result;
    }
    
    public async Task DeleteInvitationAsync(long invitationId)
    {
        using var scope = LogMethodEntry(nameof(DeleteInvitationAsync), new Dictionary<string, object>
        {
            ["InvitationId"] = invitationId
        });
        
        var existing = await _invitationRepo.Invitations.FindByIdAsync(invitationId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Invitation with ID {invitationId} not found");
        }
        
        // Check for existing reservation
        var reservations = await _reservationRepo.GetByInvitationIdAsync(invitationId);
        if (reservations.Any())
        {
            LogBusinessRuleViolation("Cannot delete invitation with reservation", "DeleteInvitation", 
                new { InvitationId = invitationId, ReservationCount = reservations.Count() });
            throw new InvalidOperationException("Cannot delete invitation that has a reservation");
        }
        
        var now = Clock.GetCurrentInstant();
        var deletedRecord = existing with { DeletedAt = now };
        
        await _crudRepo.UpdateAsync(deletedRecord);
        
        LogOperationComplete("DeleteInvitation", new { InvitationId = invitationId });
    }
    
    public async Task<string> GenerateInvitationSlugAsync(string name, long eventId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        var baseSlug = GenerateSlug(name);
        
        // Check uniqueness within the event context
        return await EnsureUniqueSlugAsync(baseSlug, async slug =>
        {
            var existingInvitations = await _invitationRepo.GetByEventIdAsync(eventId);
            return existingInvitations.Any(i => i.Slug == slug);
        });
    }
    
    public async Task<InvitationRecord?> GetInvitationBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty", nameof(slug));
        
        return await _invitationRepo.GetBySlugAsync(slug);
    }
    
    public async Task<List<InvitationRecord>> GetEventInvitationsAsync(long eventId, int skip = 0, int take = 20)
    {
        ValidatePageParameters(skip, take);
        
        using var scope = LogMethodEntry(nameof(GetEventInvitationsAsync), new Dictionary<string, object>
        {
            ["EventId"] = eventId,
            ["Skip"] = skip,
            ["Take"] = take
        });
        
        var invitations = await _invitationRepo.GetByEventIdAsync(eventId, skip, take);
        return invitations.ToList();
    }
    
    public async Task<InvitationRecord?> GetInvitationWithReservationAsync(string slug)
    {
        using var scope = LogMethodEntry(nameof(GetInvitationWithReservationAsync), new Dictionary<string, object>
        {
            ["Slug"] = slug
        });
        
        var invitation = await _invitationRepo.GetBySlugWithDetailsAsync(slug);
        if (invitation == null)
            return null;
        
        // Note: In a more complex system, we would load reservation data here
        // For now, the client can call ReservationService.GetReservationByInvitationAsync separately
        
        return invitation;
    }
    
    public async Task MarkInvitationSentAsync(long invitationId)
    {
        using var scope = LogMethodEntry(nameof(MarkInvitationSentAsync), new Dictionary<string, object>
        {
            ["InvitationId"] = invitationId
        });
        
        var existing = await _invitationRepo.Invitations.FindByIdAsync(invitationId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Invitation with ID {invitationId} not found");
        }
        
        // Since we don't have contact info in the simplified model, just log
        Logger.LogInformation("Marking invitation {InvitationId} as sent", invitationId);
        
        LogOperationComplete("MarkInvitationSent", new { InvitationId = invitationId });
    }
    
    public async Task ResendInvitationAsync(long invitationId)
    {
        using var scope = LogMethodEntry(nameof(ResendInvitationAsync), new Dictionary<string, object>
        {
            ["InvitationId"] = invitationId
        });
        
        var existing = await _invitationRepo.Invitations.FindByIdAsync(invitationId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Invitation with ID {invitationId} not found");
        }
        
        // Mark as resent
        await MarkInvitationSentAsync(invitationId);
        
        LogOperationComplete("ResendInvitation", new { InvitationId = invitationId });
    }
    
    public Task<bool> ValidateContactInfoAsync(InvitationStub stub)
    {
        // Since the InvitationStub only has Slug and EventId, this is always valid
        // In the actual implementation, this would check contact fields
        return Task.FromResult(true);
    }
}