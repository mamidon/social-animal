using NodaTime;

namespace SocialAnimal.Core.Domain;

public abstract record BaseRecord
{
    public required long Id { get; init; }
    public required Instant CreatedOn { get; init; }
    public Instant? UpdatedOn { get; init; }
    public DateTime? ConcurrencyToken { get; init; }
}