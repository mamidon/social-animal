using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Infrastructure.Services;

public class EventService : ServiceBase, IEventService
{
    private readonly IEventRepo _eventRepo;
    private readonly IInvitationRepo _invitationRepo;
    private readonly ICrudRepo _crudRepo;
    
    public EventService(
        IEventRepo eventRepo,
        IInvitationRepo invitationRepo,
        ICrudRepo crudRepo,
        ILoggerPortal logger,
        IClock clock)
        : base(logger, clock)
    {
        _eventRepo = eventRepo;
        _invitationRepo = invitationRepo;
        _crudRepo = crudRepo;
    }
    
    public async Task<EventRecord> CreateEventAsync(EventStub stub)
    {
        using var scope = LogMethodEntry(nameof(CreateEventAsync), new Dictionary<string, object>
        {
            ["Title"] = stub.Title,
            ["City"] = stub.City,
            ["State"] = stub.State
        });
        
        // Validate address
        if (!await ValidateAddressAsync(stub))
        {
            LogBusinessRuleViolation("Address validation failed", "CreateEvent", stub);
            throw new ArgumentException("Invalid address information provided");
        }
        
        // Generate unique slug
        var baseSlug = GenerateSlug(stub.Title);
        var uniqueSlug = await EnsureUniqueSlugAsync(baseSlug, _eventRepo.SlugExistsAsync);
        
        // Create event record
        var now = Clock.GetCurrentInstant();
        var eventRecord = new EventRecord
        {
            Id = 0, // Will be set by database
            Slug = uniqueSlug,
            Title = stub.Title,
            AddressLine1 = stub.AddressLine1,
            AddressLine2 = stub.AddressLine2,
            City = stub.City,
            State = stub.State.ToUpperInvariant(),
            Postal = stub.Postal,
            CreatedOn = now,
            UpdatedOn = null,
            DeletedAt = null
        };
        
        var result = await _crudRepo.CreateAsync(eventRecord);
        
        LogOperationComplete("CreateEvent", new { EventId = result.Id, Slug = result.Slug });
        return result;
    }
    
    public async Task<EventRecord> UpdateEventAsync(long eventId, EventStub stub)
    {
        using var scope = LogMethodEntry(nameof(UpdateEventAsync), new Dictionary<string, object>
        {
            ["EventId"] = eventId,
            ["Title"] = stub.Title
        });
        
        var existing = await _eventRepo.Events.FindByIdAsync(eventId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Event with ID {eventId} not found");
        }
        
        // Validate address
        if (!await ValidateAddressAsync(stub))
        {
            LogBusinessRuleViolation("Address validation failed", "UpdateEvent", stub);
            throw new ArgumentException("Invalid address information provided");
        }
        
        // Generate new slug if title changed
        string newSlug = existing.Slug;
        if (stub.Title != existing.Title)
        {
            var baseSlug = GenerateSlug(stub.Title);
            newSlug = await EnsureUniqueSlugAsync(baseSlug, async slug => 
                slug != existing.Slug && await _eventRepo.SlugExistsAsync(slug));
        }
        
        var now = Clock.GetCurrentInstant();
        var updatedRecord = existing with
        {
            Slug = newSlug,
            Title = stub.Title,
            AddressLine1 = stub.AddressLine1,
            AddressLine2 = stub.AddressLine2,
            City = stub.City,
            State = stub.State.ToUpperInvariant(),
            Postal = stub.Postal,
            UpdatedOn = now
        };
        
        var result = await _crudRepo.UpdateAsync(updatedRecord);
        
        LogOperationComplete("UpdateEvent", new { EventId = result.Id, Slug = result.Slug });
        return result;
    }
    
    public async Task DeleteEventAsync(long eventId)
    {
        using var scope = LogMethodEntry(nameof(DeleteEventAsync), new Dictionary<string, object>
        {
            ["EventId"] = eventId
        });
        
        var existing = await _eventRepo.Events.FindByIdAsync(eventId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Event with ID {eventId} not found");
        }
        
        // Check for existing invitations
        var invitations = await _invitationRepo.GetByEventIdAsync(eventId);
        if (invitations.Any())
        {
            LogBusinessRuleViolation("Cannot delete event with invitations", "DeleteEvent", 
                new { EventId = eventId, InvitationCount = invitations.Count() });
            throw new InvalidOperationException("Cannot delete event that has invitations");
        }
        
        var now = Clock.GetCurrentInstant();
        var deletedRecord = existing with { DeletedAt = now };
        
        await _crudRepo.UpdateAsync(deletedRecord);
        
        LogOperationComplete("DeleteEvent", new { EventId = eventId });
    }
    
    public async Task RestoreEventAsync(long eventId)
    {
        using var scope = LogMethodEntry(nameof(RestoreEventAsync), new Dictionary<string, object>
        {
            ["EventId"] = eventId
        });
        
        var existing = await _eventRepo.Events.FindByIdAsync(eventId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Event with ID {eventId} not found");
        }
        
        if (existing.DeletedAt == null)
        {
            throw new InvalidOperationException($"Event with ID {eventId} is not deleted");
        }
        
        var restoredRecord = existing with { DeletedAt = null };
        await _crudRepo.UpdateAsync(restoredRecord);
        
        LogOperationComplete("RestoreEvent", new { EventId = eventId });
    }
    
    public async Task<string> GenerateUniqueSlugAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        
        var baseSlug = GenerateSlug(title);
        return await EnsureUniqueSlugAsync(baseSlug, _eventRepo.SlugExistsAsync);
    }
    
    public async Task<EventRecord?> GetEventWithInvitationsAsync(string slug)
    {
        using var scope = LogMethodEntry(nameof(GetEventWithInvitationsAsync), new Dictionary<string, object>
        {
            ["Slug"] = slug
        });
        
        var eventRecord = await _eventRepo.GetBySlugAsync(slug);
        if (eventRecord == null)
            return null;
        
        // Note: In a more complex system, we would load invitations here
        // For now, the client can call InvitationService.GetEventInvitationsAsync separately
        
        return eventRecord;
    }
    
    public async Task<List<EventRecord>> GetUpcomingEventsAsync(int skip = 0, int take = 20)
    {
        ValidatePageParameters(skip, take);
        
        using var scope = LogMethodEntry(nameof(GetUpcomingEventsAsync), new Dictionary<string, object>
        {
            ["Skip"] = skip,
            ["Take"] = take
        });
        
        var events = await _eventRepo.GetActiveEventsAsync(skip, take);
        return events.ToList();
    }
    
    public async Task<List<EventRecord>> GetPastEventsAsync(int skip = 0, int take = 20)
    {
        ValidatePageParameters(skip, take);
        
        using var scope = LogMethodEntry(nameof(GetPastEventsAsync), new Dictionary<string, object>
        {
            ["Skip"] = skip,
            ["Take"] = take
        });
        
        var events = await _eventRepo.GetActiveEventsAsync(skip, take);
        return events.ToList();
    }
    
    public async Task<EventRecord?> GetEventBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty", nameof(slug));
        
        return await _eventRepo.GetBySlugAsync(slug);
    }
    
    public Task<bool> ValidateAddressAsync(EventStub stub)
    {
        if (string.IsNullOrWhiteSpace(stub.AddressLine1))
            return Task.FromResult(false);
        
        if (string.IsNullOrWhiteSpace(stub.City))
            return Task.FromResult(false);
        
        if (!ValidateStateCode(stub.State))
            return Task.FromResult(false);
        
        if (!ValidatePostalCode(stub.Postal))
            return Task.FromResult(false);
        
        return Task.FromResult(true);
    }
}