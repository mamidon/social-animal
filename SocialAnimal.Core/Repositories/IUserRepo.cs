using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IUserRepo
{
    ICrudQueries<UserRecord> Users { get; }
    Task<UserRecord?> GetBySlugAsync(string slug);
    Task<UserRecord?> GetByPhoneAsync(string phone);
    Task<bool> SlugExistsAsync(string slug);
    Task<IEnumerable<UserRecord>> GetActiveUsersAsync(int skip = 0, int take = 20);
    Task<IEnumerable<UserRecord>> GetDeletedUsersAsync(int skip = 0, int take = 20);
}