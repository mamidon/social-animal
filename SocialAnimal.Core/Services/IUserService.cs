using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Core.Services;

public interface IUserService
{
    /// <summary>
    /// Creates a new user with phone validation
    /// </summary>
    Task<UserRecord> CreateUserAsync(UserStub stub);
    
    /// <summary>
    /// Updates an existing user
    /// </summary>
    Task<UserRecord> UpdateUserAsync(long userId, UserStub stub);
    
    /// <summary>
    /// Soft deletes a user (only if no active reservations)
    /// </summary>
    Task DeleteUserAsync(long userId);
    
    /// <summary>
    /// Restores a soft-deleted user
    /// </summary>
    Task RestoreUserAsync(long userId);
    
    /// <summary>
    /// Generates a unique slug from the user name
    /// </summary>
    Task<string> GenerateUserSlugAsync(string name);
    
    /// <summary>
    /// Finds user by phone number
    /// </summary>
    Task<UserRecord?> GetUserByPhoneAsync(string phone);
    
    /// <summary>
    /// Finds user by slug
    /// </summary>
    Task<UserRecord?> GetUserBySlugAsync(string slug);
    
    /// <summary>
    /// Gets user with their reservations
    /// </summary>
    Task<UserRecord?> GetUserWithReservationsAsync(long userId);
    
    /// <summary>
    /// Merges duplicate users (combines reservations and deletes source)
    /// </summary>
    Task<UserRecord> MergeUsersAsync(long sourceUserId, long targetUserId);
    
    /// <summary>
    /// Gets user by ID
    /// </summary>
    Task<UserRecord?> GetUserByIdAsync(long userId);
    
    /// <summary>
    /// Normalizes and validates phone number format
    /// </summary>
    Task<string> NormalizePhoneNumberAsync(string phone);
}