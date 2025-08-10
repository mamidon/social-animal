using NodaTime;

namespace SocialAnimal.Core.Domain;

public record InvitationRecord : BaseRecord
{
    public required string Slug { get; init; }
    public required long EventId { get; init; }
    public Instant? DeletedAt { get; init; }
    
    // Computed properties
    public bool IsDeleted => DeletedAt.HasValue;
    
    // Related entity records (populated when needed)
    public EventRecord? Event { get; init; }
    public ICollection<ReservationRecord>? Reservations { get; init; }
}