using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record ReservationStub
{
    [Required]
    public required long InvitationId { get; init; }
    
    [Required]
    public required long UserId { get; init; }
    
    [Required]
    [Range(0, 100)] // Reasonable max party size
    public required uint PartySize { get; init; }
}

public record ReservationUpdateStub
{
    [Required]
    [Range(0, 100)]
    public required uint PartySize { get; init; }
}