using NodaTime;

namespace SocialAnimal.Core.Domain;

public record UserRecord : BaseRecord
{
    public required string Handle { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Reference { get; init; }
    public string? PasswordHash { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsEmailVerified { get; init; } = false;
    
    public string FullName => $"{FirstName} {LastName}";
}