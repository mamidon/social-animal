using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Infrastructure.Services;

public class ReservationService : ServiceBase, IReservationService
{
    private readonly IReservationRepo _reservationRepo;
    private readonly IInvitationRepo _invitationRepo;
    private readonly IUserRepo _userRepo;
    private readonly ICrudRepo _crudRepo;
    
    public ReservationService(
        IReservationRepo reservationRepo,
        IInvitationRepo invitationRepo,
        IUserRepo userRepo,
        ICrudRepo crudRepo,
        ILoggerPortal logger,
        IClock clock)
        : base(logger, clock)
    {
        _reservationRepo = reservationRepo;
        _invitationRepo = invitationRepo;
        _userRepo = userRepo;
        _crudRepo = crudRepo;
    }
    
    public async Task<ReservationRecord> CreateReservationAsync(ReservationStub stub)
    {
        using var scope = LogMethodEntry(nameof(CreateReservationAsync), new Dictionary<string, object>
        {
            ["InvitationId"] = stub.InvitationId,
            ["PartySize"] = stub.PartySize,
            ["UserId"] = stub.UserId
        });
        
        // Validate invitation exists
        var invitation = await _invitationRepo.Invitations.FindByIdAsync(stub.InvitationId);
        if (invitation == null)
        {
            throw new InvalidOperationException($"Invitation with ID {stub.InvitationId} not found");
        }
        
        // Check for existing reservation
        var existing = await _reservationRepo.GetByInvitationIdAsync(stub.InvitationId);
        if (existing.Any())
        {
            LogBusinessRuleViolation("Only one reservation per invitation", "CreateReservation", 
                new { InvitationId = stub.InvitationId });
            throw new InvalidOperationException("Only one reservation allowed per invitation");
        }
        
        // Validate party size
        if (!await ValidatePartySizeAsync(stub))
        {
            LogBusinessRuleViolation("Party size validation failed", "CreateReservation", stub);
            throw new ArgumentException("Invalid party size for this invitation");
        }
        
        // Validate user exists
        var user = await _userRepo.Users.FindByIdAsync(stub.UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {stub.UserId} not found");
        }
        
        var now = Clock.GetCurrentInstant();
        var reservationRecord = new ReservationRecord
        {
            Id = 0, // Will be set by database
            InvitationId = stub.InvitationId,
            UserId = stub.UserId,
            PartySize = stub.PartySize,
            CreatedOn = now,
            UpdatedOn = null
        };
        
        var result = await _crudRepo.CreateAsync(reservationRecord);
        
        LogOperationComplete("CreateReservation", new { ReservationId = result.Id });
        return result;
    }
    
    public async Task<ReservationRecord> UpdateReservationAsync(long reservationId, ReservationStub stub)
    {
        using var scope = LogMethodEntry(nameof(UpdateReservationAsync), new Dictionary<string, object>
        {
            ["ReservationId"] = reservationId,
            ["PartySize"] = stub.PartySize
        });
        
        var existing = await _reservationRepo.Reservations.FindByIdAsync(reservationId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Reservation with ID {reservationId} not found");
        }
        
        // Get the invitation to check constraints
        var invitation = await _invitationRepo.Invitations.FindByIdAsync(existing.InvitationId);
        if (invitation == null)
        {
            throw new InvalidOperationException($"Associated invitation not found");
        }
        
        // Validate party size
        var validationStub = stub with { InvitationId = existing.InvitationId };
        if (!await ValidatePartySizeAsync(validationStub))
        {
            LogBusinessRuleViolation("Party size validation failed", "UpdateReservation", stub);
            throw new ArgumentException("Invalid party size for this invitation");
        }
        
        var now = Clock.GetCurrentInstant();
        var updatedRecord = existing with
        {
            PartySize = stub.PartySize,
            UserId = stub.UserId,
            UpdatedOn = now
        };
        
        var result = await _crudRepo.UpdateAsync(updatedRecord);
        
        LogOperationComplete("UpdateReservation", new { ReservationId = result.Id });
        return result;
    }
    
    public async Task DeleteReservationAsync(long reservationId)
    {
        using var scope = LogMethodEntry(nameof(DeleteReservationAsync), new Dictionary<string, object>
        {
            ["ReservationId"] = reservationId
        });
        
        var existing = await _reservationRepo.Reservations.FindByIdAsync(reservationId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Reservation with ID {reservationId} not found");
        }
        
        await _crudRepo.DeleteAsync<ReservationRecord>(reservationId);
        
        LogOperationComplete("DeleteReservation", new { ReservationId = reservationId });
    }
    
    public async Task<ReservationRecord?> GetReservationByInvitationAsync(long invitationId)
    {
        var reservations = await _reservationRepo.GetByInvitationIdAsync(invitationId);
        return reservations.FirstOrDefault();
    }
    
    public async Task<List<ReservationRecord>> GetEventReservationsAsync(long eventId)
    {
        using var scope = LogMethodEntry(nameof(GetEventReservationsAsync), new Dictionary<string, object>
        {
            ["EventId"] = eventId
        });
        
        // Get all invitations for the event first
        var invitations = await _invitationRepo.GetByEventIdAsync(eventId);
        var reservations = new List<ReservationRecord>();
        
        foreach (var invitation in invitations)
        {
            var invitationReservations = await _reservationRepo.GetByInvitationIdAsync(invitation.Id);
            reservations.AddRange(invitationReservations);
        }
        
        return reservations;
    }
    
    public async Task<List<ReservationRecord>> GetUserReservationsAsync(long userId)
    {
        using var scope = LogMethodEntry(nameof(GetUserReservationsAsync), new Dictionary<string, object>
        {
            ["UserId"] = userId
        });
        
        var reservations = await _reservationRepo.GetByUserIdAsync(userId);
        return reservations.ToList();
    }
    
    public async Task<int> CalculateEventAttendanceAsync(long eventId)
    {
        using var scope = LogMethodEntry(nameof(CalculateEventAttendanceAsync), new Dictionary<string, object>
        {
            ["EventId"] = eventId
        });
        
        var reservations = await GetEventReservationsAsync(eventId);
        
        // Sum party sizes, excluding "regrets" (party size 0)
        var totalAttendance = reservations
            .Where(r => r.PartySize > 0)
            .Sum(r => (int)r.PartySize);
        
        LogOperationComplete("CalculateEventAttendance", new { EventId = eventId, TotalAttendance = totalAttendance });
        return totalAttendance;
    }
    
    public async Task SendReminderAsync(long reservationId)
    {
        using var scope = LogMethodEntry(nameof(SendReminderAsync), new Dictionary<string, object>
        {
            ["ReservationId"] = reservationId
        });
        
        var reservation = await _reservationRepo.Reservations.FindByIdAsync(reservationId);
        if (reservation == null)
        {
            throw new InvalidOperationException($"Reservation with ID {reservationId} not found");
        }
        
        var invitation = await _invitationRepo.Invitations.FindByIdAsync(reservation.InvitationId);
        if (invitation == null)
        {
            throw new InvalidOperationException("Associated invitation not found");
        }
        
        // In a real implementation, this would send email/SMS
        // For now, just log the reminder
        Logger.LogInformation("Reminder sent for reservation {ReservationId} for invitation {InvitationSlug}", 
            reservationId, invitation.Slug);
        
        LogOperationComplete("SendReminder", new { ReservationId = reservationId });
    }
    
    public async Task MarkAsAttendedAsync(long reservationId)
    {
        using var scope = LogMethodEntry(nameof(MarkAsAttendedAsync), new Dictionary<string, object>
        {
            ["ReservationId"] = reservationId
        });
        
        var existing = await _reservationRepo.Reservations.FindByIdAsync(reservationId);
        if (existing == null)
        {
            throw new InvalidOperationException($"Reservation with ID {reservationId} not found");
        }
        
        if (existing.PartySize == 0)
        {
            LogBusinessRuleViolation("Cannot mark regret as attended", "MarkAttended", existing);
            throw new InvalidOperationException("Cannot mark 'regrets' reservation as attended");
        }
        
        // Since we don't have an AttendedOn field in the simplified model, just log
        Logger.LogInformation("Marking reservation {ReservationId} as attended", reservationId);
        
        LogOperationComplete("MarkAsAttended", new { ReservationId = reservationId });
    }
    
    public async Task<ReservationRecord?> GetReservationByIdAsync(long reservationId)
    {
        return await _reservationRepo.Reservations.FindByIdAsync(reservationId);
    }
    
    public Task<bool> ValidatePartySizeAsync(ReservationStub stub)
    {
        // Party size must be valid uint (0 is valid for "regrets")
        if (stub.PartySize > 100) // Reasonable upper limit
            return Task.FromResult(false);
        
        // All party sizes are valid in the simplified model
        // In a more complex model, we would check against invitation limits
        return Task.FromResult(true);
    }
}