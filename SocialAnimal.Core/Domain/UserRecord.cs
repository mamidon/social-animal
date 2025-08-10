using NodaTime;

namespace SocialAnimal.Core.Domain;

public record UserRecord : BaseRecord
{
    public required string Slug { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Phone { get; init; } // E164 format: "+14256987637"
    public Instant? DeletedAt { get; init; }
    
    public string FullName => $"{FirstName} {LastName}";
    public bool IsDeleted => DeletedAt.HasValue;
}