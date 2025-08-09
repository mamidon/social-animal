using NodaTime;

namespace SocialAnimal.Core.Portals;

public interface IClockPortal
{
    Instant Now { get; }
    LocalDate Today { get; }
    DateTimeZone SystemTimeZone { get; }
    
    // Time zone conversions
    ZonedDateTime InTimeZone(Instant instant, string timeZoneId);
    ZonedDateTime InSystemTimeZone(Instant instant);
    
    // Duration calculations
    Duration DurationBetween(Instant start, Instant end);
    Instant Add(Instant instant, Duration duration);
}