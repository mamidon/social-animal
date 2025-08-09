using NodaTime;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Portals;

public class SystemClockPortal : IClockPortal
{
    private readonly IClock _clock;
    private readonly DateTimeZone _systemTimeZone;
    
    public SystemClockPortal(IClock clock)
    {
        _clock = clock;
        _systemTimeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
    }
    
    public Instant Now => _clock.GetCurrentInstant();
    
    public LocalDate Today => Now.InZone(_systemTimeZone).Date;
    
    public DateTimeZone SystemTimeZone => _systemTimeZone;
    
    public ZonedDateTime InTimeZone(Instant instant, string timeZoneId)
    {
        var timeZone = DateTimeZoneProviders.Tzdb[timeZoneId];
        return instant.InZone(timeZone);
    }
    
    public ZonedDateTime InSystemTimeZone(Instant instant)
    {
        return instant.InZone(_systemTimeZone);
    }
    
    public Duration DurationBetween(Instant start, Instant end)
    {
        return end - start;
    }
    
    public Instant Add(Instant instant, Duration duration)
    {
        return instant.Plus(duration);
    }
}