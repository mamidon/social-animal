using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Handle), IsUnique = true)]
[Index(nameof(Reference), IsUnique = true)]
public class User : BaseEntity
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
}