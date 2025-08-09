using SocialAnimal.Core.Domain;

namespace SocialAnimal.Core.Repositories;

public interface IUserRepo : ICrudRepo
{
    ICrudQueries<UserRecord> Users { get; }
    Task<UserRecord?> FindByEmailAsync(string email);
    Task<UserRecord?> FindByHandleAsync(string handle);
    Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null);
    Task<bool> IsHandleUniqueAsync(string handle, long? excludeUserId = null);
}