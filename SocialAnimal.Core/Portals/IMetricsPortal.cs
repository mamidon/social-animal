using NodaTime;

namespace SocialAnimal.Core.Portals;

public interface IMetricsPortal
{
    // Counter metrics
    void IncrementCounter(string name, long value = 1, Dictionary<string, string>? tags = null);
    
    // Gauge metrics
    void RecordGauge(string name, double value, Dictionary<string, string>? tags = null);
    
    // Histogram/Timer metrics
    IDisposable MeasureDuration(string name, Dictionary<string, string>? tags = null);
    void RecordDuration(string name, Duration duration, Dictionary<string, string>? tags = null);
    
    // Custom events
    void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null);
    
    // Availability tracking
    void TrackAvailability(string name, bool isAvailable, Duration? duration = null);
}

public interface IDurationMeasurement : IDisposable
{
    void AddTag(string key, string value);
    void SetSuccess(bool success);
}