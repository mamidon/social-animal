using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Core.Services;

public interface IReservationService
{
    /// <summary>
    /// Creates a new RSVP reservation
    /// </summary>
    Task<ReservationRecord> CreateReservationAsync(ReservationStub stub);
    
    /// <summary>
    /// Updates an existing reservation
    /// </summary>
    Task<ReservationRecord> UpdateReservationAsync(long reservationId, ReservationStub stub);
    
    /// <summary>
    /// Removes a reservation
    /// </summary>
    Task DeleteReservationAsync(long reservationId);
    
    /// <summary>
    /// Gets reservation by invitation
    /// </summary>
    Task<ReservationRecord?> GetReservationByInvitationAsync(long invitationId);
    
    /// <summary>
    /// Gets all reservations for an event
    /// </summary>
    Task<List<ReservationRecord>> GetEventReservationsAsync(long eventId);
    
    /// <summary>
    /// Gets all reservations for a user
    /// </summary>
    Task<List<ReservationRecord>> GetUserReservationsAsync(long userId);
    
    /// <summary>
    /// Calculates total attendance count for an event
    /// </summary>
    Task<int> CalculateEventAttendanceAsync(long eventId);
    
    /// <summary>
    /// Sends a reminder for a reservation
    /// </summary>
    Task SendReminderAsync(long reservationId);
    
    /// <summary>
    /// Marks a reservation as attended
    /// </summary>
    Task MarkAsAttendedAsync(long reservationId);
    
    /// <summary>
    /// Gets reservation by ID
    /// </summary>
    Task<ReservationRecord?> GetReservationByIdAsync(long reservationId);
    
    /// <summary>
    /// Validates party size against invitation limits
    /// </summary>
    Task<bool> ValidatePartySizeAsync(ReservationStub stub);
}