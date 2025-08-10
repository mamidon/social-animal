# Task 1: Update User Entity and Domain Model

## Objective
Refactor the existing User entity and UserRecord domain model to match the new requirements, replacing email with phone number and updating naming conventions.

## Requirements Changes
- Replace `Email` field with `Phone` field (E164 format)
- Rename `Handle` to `Slug` for consistency
- Remove `Reference` field (not in new requirements)
- Add `DeletedAt` field for soft delete support
- Remove authentication-related fields (`PasswordHash`, `IsEmailVerified`)
- Simplify to match exact schema requirements

## Implementation Steps

### Step 1: Update UserRecord Domain Model
Location: `/SocialAnimal.Core/Domain/UserRecord.cs`

```csharp
using NodaTime;

namespace SocialAnimal.Core.Domain;

public record UserRecord : BaseRecord
{
    public required string Slug { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Phone { get; init; } // E164 format: "+14256987637"
    public Instant? DeletedAt { get; init; }
    
    public string FullName => $"{FirstName} {LastName}";
    public bool IsDeleted => DeletedAt.HasValue;
}
```

### Step 2: Update User Entity
Location: `/SocialAnimal.Infrastructure/Db/Entities/User.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(Phone))]
[Index(nameof(DeletedAt))]
public class User : BaseEntity, IInto<UserRecord>, IFrom<User, UserRecord>
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; } // Opaque public identifier
    
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }
    
    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone must be in E164 format")]
    public required string Phone { get; set; } // E164 format
    
    public Instant? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    
    // Mapping implementations
    public UserRecord Into()
    {
        return new UserRecord
        {
            Id = Id,
            Slug = Slug,
            FirstName = FirstName,
            LastName = LastName,
            Phone = Phone,
            DeletedAt = DeletedAt,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken
        };
    }
    
    public static UserRecord From(User entity)
    {
        return entity.Into();
    }
    
    public static User FromRecord(UserRecord record)
    {
        return new User
        {
            Id = record.Id,
            Slug = Slug,
            FirstName = record.FirstName,
            LastName = record.LastName,
            Phone = record.Phone,
            DeletedAt = record.DeletedAt,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}
```

### Step 3: Create UserStub for Creation/Updates
Location: `/SocialAnimal.Core/Stubs/UserStub.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record UserStub
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; init; }
    
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }
    
    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }
    
    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone must be in E164 format")]
    public required string Phone { get; init; }
}
```

### Step 4: Update UserConfiguration
Location: `/SocialAnimal.Infrastructure/Db/Context/Configuration/UserConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Db.Context.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        
        // Primary key
        builder.HasKey(u => u.Id);
        
        // Indexes
        builder.HasIndex(u => u.Slug)
            .IsUnique()
            .HasDatabaseName("ix_users_slug");
            
        builder.HasIndex(u => u.Phone)
            .HasDatabaseName("ix_users_phone");
            
        builder.HasIndex(u => u.DeletedAt)
            .HasDatabaseName("ix_users_deleted_at");
        
        // Properties
        builder.Property(u => u.Slug)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(u => u.Phone)
            .IsRequired()
            .HasMaxLength(20);
            
        // Soft delete query filter
        builder.HasQueryFilter(u => u.DeletedAt == null);
        
        // Concurrency token
        builder.Property(u => u.ConcurrencyToken)
            .IsConcurrencyToken();
    }
}
```

### Step 5: Update IUserRepo Interface
Location: `/SocialAnimal.Core/Repositories/IUserRepo.cs`

```csharp
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
```

### Step 6: Update UserRepo Implementation
Location: `/SocialAnimal.Infrastructure/Repositories/UserRepo.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;
using SocialAnimal.Infrastructure.Db.Context;
using SocialAnimal.Infrastructure.Db.Entities;

namespace SocialAnimal.Infrastructure.Repositories;

public class UserRepo : IUserRepo
{
    private readonly Func<ApplicationContext> _unitOfWork;
    
    public UserRepo(Func<ApplicationContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Users = new CrudQueries<ApplicationContext, User, UserRecord>(
            unitOfWork, c => c.Users);
    }
    
    public ICrudQueries<UserRecord> Users { get; }
    
    public async Task<UserRecord?> GetBySlugAsync(string slug)
    {
        using var context = _unitOfWork();
        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Slug == slug);
        return user?.Into();
    }
    
    public async Task<UserRecord?> GetByPhoneAsync(string phone)
    {
        using var context = _unitOfWork();
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone);
        return user?.Into();
    }
    
    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var context = _unitOfWork();
        return await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Slug == slug);
    }
    
    public async Task<IEnumerable<UserRecord>> GetActiveUsersAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var users = await context.Users
            .OrderByDescending(u => u.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return users.Select(u => u.Into());
    }
    
    public async Task<IEnumerable<UserRecord>> GetDeletedUsersAsync(int skip = 0, int take = 20)
    {
        using var context = _unitOfWork();
        var users = await context.Users
            .IgnoreQueryFilters()
            .Where(u => u.DeletedAt != null)
            .OrderByDescending(u => u.DeletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return users.Select(u => u.Into());
    }
}
```

## Testing Checklist

- [ ] Unit test for UserRecord creation and property access
- [ ] Unit test for User entity to UserRecord mapping
- [ ] Unit test for UserRecord to User entity mapping
- [ ] Integration test for UserRepo CRUD operations
- [ ] Integration test for soft delete functionality
- [ ] Integration test for unique slug constraint
- [ ] Validation test for E164 phone format
- [ ] Test query filters exclude deleted users by default
- [ ] Test IgnoreQueryFilters includes deleted users

## Migration Considerations

If there's existing data in the User table:
1. Back up existing data before migration
2. Create data migration script to:
   - Generate slug values from existing Handle values
   - Set DeletedAt to null for all existing records
   - Handle missing phone numbers (may need temporary default)
3. Update any existing code that references old field names
4. Update API controllers and services that use User entity

## Validation Rules

- Slug: Required, max 100 chars, globally unique (including soft-deleted)
- FirstName: Required, max 100 chars
- LastName: Required, max 100 chars  
- Phone: Required, max 20 chars, must match E164 format regex
- DeletedAt: Optional, when set marks record as soft-deleted

## Dependencies

This task must be completed before:
- Task 4 (Reservation entity needs User FK)
- Task 6 (Database migration generation)
- Task 7 (Repository layer updates)

## Notes

- The slug should be URL-safe and human-readable (e.g., "john-doe-1234")
- Consider adding a slug generation service in a future task
- Phone validation should accept international numbers in E164 format
- Soft-deleted users should not appear in normal queries but should be accessible for historical data