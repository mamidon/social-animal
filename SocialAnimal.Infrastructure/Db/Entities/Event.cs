using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(DeletedAt))]
public class Event : BaseEntity, IInto<EventRecord>, IFrom<Event, EventRecord>
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; } // Opaque public identifier
    
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }
    
    [Required]
    [MaxLength(200)]
    public required string AddressLine1 { get; set; }
    
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string City { get; set; }
    
    [Required]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "State must be a two-letter code")]
    public required string State { get; set; }
    
    [Required]
    [MaxLength(20)]
    public required string Postal { get; set; }
    
    public Instant? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    
    // Mapping implementations
    public EventRecord Into()
    {
        return new EventRecord
        {
            Id = Id,
            Slug = Slug,
            Title = Title,
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            City = City,
            State = State,
            Postal = Postal,
            DeletedAt = DeletedAt,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken
        };
    }
    
    public static EventRecord From(Event entity)
    {
        return entity.Into();
    }
    
    public static Event FromRecord(EventRecord record)
    {
        return new Event
        {
            Id = record.Id,
            Slug = record.Slug,
            Title = record.Title,
            AddressLine1 = record.AddressLine1,
            AddressLine2 = record.AddressLine2,
            City = record.City,
            State = record.State,
            Postal = record.Postal,
            DeletedAt = record.DeletedAt,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}