using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SocialAnimal.Core.Domain;
using SocialAnimal.Core.Repositories;

namespace SocialAnimal.Infrastructure.Db.Entities;

[Index(nameof(InvitationId), nameof(UserId), IsUnique = true)]
[Index(nameof(InvitationId))]
[Index(nameof(UserId))]
public class Reservation : BaseEntity, IInto<ReservationRecord>, IFrom<Reservation, ReservationRecord>
{
    [Required]
    public required long InvitationId { get; set; }
    
    [Required]
    public required long UserId { get; set; }
    
    [Required]
    [Range(0, uint.MaxValue)]
    public required uint PartySize { get; set; } // 0 = sends regrets
    
    // Navigation properties
    public virtual Invitation Invitation { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    
    // Mapping implementations
    public ReservationRecord Into()
    {
        return new ReservationRecord
        {
            Id = Id,
            InvitationId = InvitationId,
            UserId = UserId,
            PartySize = PartySize,
            CreatedOn = CreatedOn,
            UpdatedOn = UpdatedOn,
            ConcurrencyToken = ConcurrencyToken,
            Invitation = Invitation?.Into(),
            User = User?.Into()
        };
    }
    
    public static ReservationRecord From(Reservation entity)
    {
        return entity.Into();
    }
    
    public static Reservation FromRecord(ReservationRecord record)
    {
        return new Reservation
        {
            Id = record.Id,
            InvitationId = record.InvitationId,
            UserId = record.UserId,
            PartySize = record.PartySize,
            CreatedOn = record.CreatedOn,
            UpdatedOn = record.UpdatedOn ?? record.CreatedOn,
            ConcurrencyToken = record.ConcurrencyToken
        };
    }
}