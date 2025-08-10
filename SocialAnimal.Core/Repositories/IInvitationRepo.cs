using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IInvitationRepo
{
    ICrudQueries<InvitationRecord> Invitations { get; }
    Task<InvitationRecord?> GetBySlugAsync(string slug);
    Task<InvitationRecord?> GetBySlugWithDetailsAsync(string slug);
    Task<bool> SlugExistsAsync(string slug);
    Task<IEnumerable<InvitationRecord>> GetByEventIdAsync(long eventId, int skip = 0, int take = 20);
    Task<IEnumerable<InvitationRecord>> GetActiveInvitationsAsync(int skip = 0, int take = 20);
    Task<IEnumerable<InvitationRecord>> GetDeletedInvitationsAsync(int skip = 0, int take = 20);
    Task<int> GetInvitationCountForEventAsync(long eventId);
}