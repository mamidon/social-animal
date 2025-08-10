using NodaTime;

namespace SocialAnimal.Core.Domain;

public record EventRecord : BaseRecord
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; } // Two-letter state code
    public required string Postal { get; init; }
    public Instant? DeletedAt { get; init; }
    
    public bool IsDeleted => DeletedAt.HasValue;
    
    public string FullAddress => string.IsNullOrWhiteSpace(AddressLine2) 
        ? $"{AddressLine1}, {City}, {State} {Postal}"
        : $"{AddressLine1}, {AddressLine2}, {City}, {State} {Postal}";
}