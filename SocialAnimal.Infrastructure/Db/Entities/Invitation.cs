using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(EventId))]
[Index(nameof(DeletedAt))]
public class Invitation : BaseEntity, IInto<InvitationRecord>, IFrom<Invitation, InvitationRecord>
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; } // Opaque public identifier
    
    [Required]
    public required long EventId { get; set; }
    
    public Instant? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual Event Event { get; set; } = null!;
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    
    // Mapping implementations
    public InvitationRecord Into()
    {
        return new InvitationRecord
        {
            Id = Id,
            Slug = Slug,
            EventId = EventId,
            DeletedAt = DeletedAt,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken,
            Event = Event?.Into(),
            Reservations = Reservations?.Select(r => r.Into()).ToList()
        };
    }
    
    public static InvitationRecord From(Invitation entity)
    {
        return entity.Into();
    }
    
    public static Invitation FromRecord(InvitationRecord record)
    {
        return new Invitation
        {
            Id = record.Id,
            Slug = record.Slug,
            EventId = record.EventId,
            DeletedAt = record.DeletedAt,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}