using NodaTime;

namespace SocialAnimal.Core.Domain;

public record UserRecord : BaseRecord
{
    public required string Email { get; init; }
    public required string Handle { get; init; }
    public required string DisplayName { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsEmailVerified { get; init; } = false;
}