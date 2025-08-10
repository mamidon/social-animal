using NodaTime;

namespace SocialAnimal.Core.Domain;

public record ReservationRecord : BaseRecord
{
    public required long InvitationId { get; init; }
    public required long UserId { get; init; }
    public required uint PartySize { get; init; } // 0 = sends regrets
    
    // Computed properties
    public bool IsAttending => PartySize > 0;
    public bool HasDeclined => PartySize == 0;
    
    // Related entity records (populated when needed)
    public InvitationRecord? Invitation { get; init; }
    public UserRecord? User { get; init; }
}