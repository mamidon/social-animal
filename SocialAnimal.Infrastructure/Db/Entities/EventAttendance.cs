using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(EventId), nameof(UserId), IsUnique = true)]
public class EventAttendance : BaseEntity
{
    public required long EventId { get; set; }
    
    public required long UserId { get; set; }
    
    public required AttendanceStatus Status { get; set; }
    
    public Instant? RespondedOn { get; set; }
    
    public Instant? CheckedInOn { get; set; }
    
    // Navigation properties
    public virtual Event Event { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public enum AttendanceStatus
{
    Invited,
    Accepted,
    Declined,
    Maybe,
    CheckedIn
}