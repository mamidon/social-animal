using System.ComponentModel.DataAnnotations;

namespace SocialAnimal.Core.Stubs;

public record InvitationStub
{
    [Required]
    [MaxLength(100)]
    public required string Slug { get; init; }
    
    [Required]
    public required long EventId { get; init; }
}

public record InvitationUpdateStub
{
    // Currently no updatable fields beyond soft delete
    // This stub exists for future extensibility
}