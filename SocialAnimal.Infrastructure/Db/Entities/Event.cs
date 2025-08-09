using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Handle), IsUnique = true)]
[Index(nameof(Reference), IsUnique = true)]
[Index(nameof(StartsOn))]
public class Event : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public required string Handle { get; set; } // URL slug for event
    
    [Required]
    [MaxLength(50)]
    public required string Reference { get; set; } // External ID like "event_xyz789"
    
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public required Instant StartsOn { get; set; }
    
    public required Instant EndsOn { get; set; }
    
    [MaxLength(500)]
    public string? Location { get; set; }
    
    public int? MaxAttendees { get; set; }
    
    public bool IsPublic { get; set; } = true;
    
    public bool IsCancelled { get; set; } = false;
    
    // Foreign keys
    public required long OrganizerId { get; set; }
    
    // Navigation properties
    public virtual User Organizer { get; set; } = null!;
    public virtual ICollection<EventAttendance> Attendances { get; set; } = new List<EventAttendance>();
}