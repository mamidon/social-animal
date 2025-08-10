using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IReservationRepo
{
    ICrudQueries<ReservationRecord> Reservations { get; }
    Task<ReservationRecord?> GetByInvitationAndUserAsync(long invitationId, long userId);
    Task<IEnumerable<ReservationRecord>> GetByInvitationIdAsync(long invitationId);
    Task<IEnumerable<ReservationRecord>> GetByUserIdAsync(long userId);
    Task<IEnumerable<ReservationRecord>> GetAcceptedReservationsAsync(long invitationId);
    Task<IEnumerable<ReservationRecord>> GetDeclinedReservationsAsync(long invitationId);
    Task<int> GetTotalPartySizeForInvitationAsync(long invitationId);
    Task<int> GetAcceptedCountForInvitationAsync(long invitationId);
    Task<int> GetDeclinedCountForInvitationAsync(long invitationId);
}