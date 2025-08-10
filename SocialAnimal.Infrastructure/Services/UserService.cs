using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Portals;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Core.Services;
using SocialAnimal.Core.Stubs;

namespace SocialAnimal.Infrastructure.Services;

public class UserService : ServiceBase, IUserService
{
    private readonly IUserRepo _userRepo;
    private readonly IReservationRepo _reservationRepo;
    private readonly ICrudRepo _crudRepo;
    
    public UserService(
        IUserRepo userRepo,
        IReservationRepo reservationRepo,
        ICrudRepo crudRepo,
        ILoggerPortal logger,
        IClock clock)
        : base(logger, clock)
    {
        _userRepo = userRepo;
        _reservationRepo = reservationRepo;
        _crudRepo = crudRepo;
    }
    
    public async Task<UserRecord> CreateUserAsync(UserStub stub)
    {
        using var scope = LogMethodEntry(nameof(CreateUserAsync), new Dictionary<string, object>
        {
            ["FirstName"] = stub.FirstName,
            ["LastName"] = stub.LastName,
            ["Phone"] = stub.Phone
        });
        
        // Normalize and validate phone
        var normalizedPhone = await NormalizePhoneNumberAsync(stub.Phone);
        
        // Check for existing user with this phone
        var existingUser = await _userRepo.GetByPhoneAsync(normalizedPhone);
        if (existingUser != null)
        {
            LogBusinessRuleViolation("Phone number already exists", "CreateUser", 
                new { Phone = normalizedPhone, ExistingUserId = existingUser.Id });
            throw new InvalidOperationException($"User with phone {normalizedPhone} already exists");
        }
        
        // Generate unique slug from full name
        var fullName = $"{stub.FirstName} {stub.LastName}";
        var slug = await GenerateUserSlugAsync(fullName);
        
        var now = Clock.GetCurrentInstant();
        var userRecord = new UserRecord
        {
            Id = 0, // Will be set by database
            Slug = slug,
            FirstName = stub.FirstName,
            LastName = stub.LastName,
            Phone = normalizedPhone,
            CreatedOn = now,
            UpdatedOn = null,
            DeletedAt = null
        };
        
        var result = await _crudRepo.CreateAsync(userRecord);
        
        LogOperationComplete("CreateUser", new { UserId = result.Id, Slug = result.Slug });
        return result;
    }
    
    public async Task<UserRecord> UpdateUserAsync(long userId, UserStub stub)
    {
        using var scope = LogMethodEntry(nameof(UpdateUserAsync), new Dictionary<string, object>
        {
            ["UserId"] = userId,
            ["FirstName"] = stub.FirstName,
            ["LastName"] = stub.LastName
        });
        
        var existing = await _userRepo.Users.FindByIdAsync(userId);
        if (existing == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }
        
        // Normalize and validate phone
        var normalizedPhone = await NormalizePhoneNumberAsync(stub.Phone);
        
        // Check for existing user with this phone (excluding current user)
        var existingUserWithPhone = await _userRepo.GetByPhoneAsync(normalizedPhone);
        if (existingUserWithPhone != null && existingUserWithPhone.Id != userId)
        {
            LogBusinessRuleViolation("Phone number already exists", "UpdateUser", 
                new { Phone = normalizedPhone, ExistingUserId = existingUserWithPhone.Id });
            throw new InvalidOperationException($"Another user with phone {normalizedPhone} already exists");
        }
        
        // Generate new slug if name changed
        var fullName = $"{stub.FirstName} {stub.LastName}";
        string newSlug = existing.Slug;
        if (fullName != existing.FullName)
        {
            newSlug = await GenerateUserSlugAsync(fullName);
        }
        
        var now = Clock.GetCurrentInstant();
        var updatedRecord = existing with
        {
            Slug = newSlug,
            FirstName = stub.FirstName,
            LastName = stub.LastName,
            Phone = normalizedPhone,
            UpdatedOn = now
        };
        
        var result = await _crudRepo.UpdateAsync(updatedRecord);
        
        LogOperationComplete("UpdateUser", new { UserId = result.Id, Slug = result.Slug });
        return result;
    }
    
    public async Task DeleteUserAsync(long userId)
    {
        using var scope = LogMethodEntry(nameof(DeleteUserAsync), new Dictionary<string, object>
        {
            ["UserId"] = userId
        });
        
        var existing = await _userRepo.Users.FindByIdAsync(userId);
        if (existing == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }
        
        // Check for active reservations
        var reservations = await _reservationRepo.GetByUserIdAsync(userId);
        if (reservations.Any())
        {
            LogBusinessRuleViolation("Cannot delete user with reservations", "DeleteUser", 
                new { UserId = userId, ReservationCount = reservations.Count() });
            throw new InvalidOperationException("Cannot delete user that has reservations");
        }
        
        var now = Clock.GetCurrentInstant();
        var deletedRecord = existing with { DeletedAt = now };
        
        await _crudRepo.UpdateAsync(deletedRecord);
        
        LogOperationComplete("DeleteUser", new { UserId = userId });
    }
    
    public async Task RestoreUserAsync(long userId)
    {
        using var scope = LogMethodEntry(nameof(RestoreUserAsync), new Dictionary<string, object>
        {
            ["UserId"] = userId
        });
        
        var existing = await _userRepo.Users.FindByIdAsync(userId);
        if (existing == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }
        
        if (existing.DeletedAt == null)
        {
            throw new InvalidOperationException($"User with ID {userId} is not deleted");
        }
        
        // Check if phone number is now taken by another user
        var existingUserWithPhone = await _userRepo.GetByPhoneAsync(existing.Phone);
        if (existingUserWithPhone != null && existingUserWithPhone.Id != userId)
        {
            LogBusinessRuleViolation("Cannot restore user - phone taken", "RestoreUser", 
                new { UserId = userId, Phone = existing.Phone });
            throw new InvalidOperationException($"Cannot restore user - phone {existing.Phone} is now used by another user");
        }
        
        var restoredRecord = existing with { DeletedAt = null };
        await _crudRepo.UpdateAsync(restoredRecord);
        
        LogOperationComplete("RestoreUser", new { UserId = userId });
    }
    
    public async Task<string> GenerateUserSlugAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        var baseSlug = GenerateSlug(name);
        return await EnsureUniqueSlugAsync(baseSlug, _userRepo.SlugExistsAsync);
    }
    
    public async Task<UserRecord?> GetUserByPhoneAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));
        
        var normalizedPhone = await NormalizePhoneNumberAsync(phone);
        return await _userRepo.GetByPhoneAsync(normalizedPhone);
    }
    
    public async Task<UserRecord?> GetUserBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty", nameof(slug));
        
        return await _userRepo.GetBySlugAsync(slug);
    }
    
    public async Task<UserRecord?> GetUserWithReservationsAsync(long userId)
    {
        using var scope = LogMethodEntry(nameof(GetUserWithReservationsAsync), new Dictionary<string, object>
        {
            ["UserId"] = userId
        });
        
        var user = await _userRepo.Users.FindByIdAsync(userId);
        if (user == null)
            return null;
        
        // Note: In a more complex system, we would load reservations here
        // For now, the client can call ReservationService.GetUserReservationsAsync separately
        
        return user;
    }
    
    public async Task<UserRecord> MergeUsersAsync(long sourceUserId, long targetUserId)
    {
        using var scope = LogMethodEntry(nameof(MergeUsersAsync), new Dictionary<string, object>
        {
            ["SourceUserId"] = sourceUserId,
            ["TargetUserId"] = targetUserId
        });
        
        if (sourceUserId == targetUserId)
        {
            throw new ArgumentException("Cannot merge user with themselves");
        }
        
        var sourceUser = await _userRepo.Users.FindByIdAsync(sourceUserId);
        if (sourceUser == null)
        {
            throw new InvalidOperationException($"Source user with ID {sourceUserId} not found");
        }
        
        var targetUser = await _userRepo.Users.FindByIdAsync(targetUserId);
        if (targetUser == null)
        {
            throw new InvalidOperationException($"Target user with ID {targetUserId} not found");
        }
        
        // Move all reservations from source to target
        var sourceReservations = await _reservationRepo.GetByUserIdAsync(sourceUserId);
        foreach (var reservation in sourceReservations)
        {
            var updatedReservation = reservation with 
            { 
                UserId = targetUserId,
                UpdatedOn = Clock.GetCurrentInstant()
            };
            await _crudRepo.UpdateAsync(updatedReservation);
        }
        
        // Soft delete the source user
        var now = Clock.GetCurrentInstant();
        var deletedSourceUser = sourceUser with { DeletedAt = now };
        await _crudRepo.UpdateAsync(deletedSourceUser);
        
        LogOperationComplete("MergeUsers", new 
        { 
            SourceUserId = sourceUserId, 
            TargetUserId = targetUserId, 
            MovedReservations = sourceReservations.Count() 
        });
        
        return targetUser;
    }
    
    public async Task<UserRecord?> GetUserByIdAsync(long userId)
    {
        return await _userRepo.Users.FindByIdAsync(userId);
    }
    
    public Task<string> NormalizePhoneNumberAsync(string phone)
    {
        return Task.FromResult(NormalizePhoneNumber(phone));
    }
}