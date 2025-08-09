using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Handle), IsUnique = true)]
[Index(nameof(Reference), IsUnique = true)]
public class User : BaseEntity, IInto<UserRecord>, IFrom<User, UserRecord>
{
    [Required]
    [MaxLength(100)]
    public required string Handle { get; set; } // URL-safe username
    
    [Required]
    [MaxLength(255)]
    public required string Email { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string Reference { get; set; } // External ID like "user_abc123"
    
    public string? PasswordHash { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsEmailVerified { get; set; } = false;
    
    // Navigation properties
    public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public virtual ICollection<EventAttendance> EventAttendances { get; set; } = new List<EventAttendance>();
    
    // Mapping implementations
    public UserRecord Into()
    {
        return new UserRecord
        {
            Id = Id,
            Handle = Handle,
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            Reference = Reference,
            PasswordHash = PasswordHash,
            IsActive = IsActive,
            IsEmailVerified = IsEmailVerified,
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
            Handle = record.Handle,
            Email = record.Email,
            FirstName = record.FirstName,
            LastName = record.LastName,
            Reference = record.Reference,
            PasswordHash = record.PasswordHash,
            IsActive = record.IsActive,
            IsEmailVerified = record.IsEmailVerified,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}