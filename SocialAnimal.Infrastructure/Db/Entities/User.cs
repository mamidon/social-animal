using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;
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
            Slug = record.Slug,
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