using System.Collections.Concurrent;
using System.Diagnostics;
using NodaTime;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Portals;

public class InMemoryMetricsPortal : IMetricsPortal
{
    private readonly ConcurrentDictionary<string, long> _counters;
    private readonly ConcurrentDictionary<string, double> _gauges;
    private readonly ConcurrentDictionary<string, List<double>> _histograms;
    private readonly IClock _clock;
    private readonly ILoggerPortal _logger;
    
    public InMemoryMetricsPortal(IClock clock, ILoggerPortal logger)
    {
        _counters = new ConcurrentDictionary<string, long>();
        _gauges = new ConcurrentDictionary<string, double>();
        _histograms = new ConcurrentDictionary<string, List<double>>();
        _clock = clock;
        _logger = logger;
    }
    
    public void IncrementCounter(string name, long value = 1, Dictionary<string, string>? tags = null)
    {
        var key = BuildMetricKey(name, tags);
        _counters.AddOrUpdate(key, value, (_, current) => current + value);
        
        _logger.LogDebug("Counter incremented: {Name} by {Value}, Total: {Total}", 
            name, value, _counters[key]);
    }
    
    public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildMetricKey(name, tags);
        _gauges[key] = value;
        
        _logger.LogDebug("Gauge recorded: {Name} = {Value}", name, value);
    }
    
    public IDisposable MeasureDuration(string name, Dictionary<string, string>? tags = null)
    {
        return new DurationMeasurement(this, name, tags, _clock);
    }
    
    public void RecordDuration(string name, Duration duration, Dictionary<string, string>? tags = null)
    {
        var key = BuildMetricKey(name, tags);
        var milliseconds = duration.TotalMilliseconds;
        
        _histograms.AddOrUpdate(key,
            new List<double> { milliseconds },
            (_, list) =>
            {
                list.Add(milliseconds);
                return list;
            });
        
        _logger.LogDebug("Duration recorded: {Name} = {Duration}ms", name, milliseconds);
    }
    
    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null)
    {
        var logProperties = new Dictionary<string, object>
        {
            ["EventName"] = eventName,
            ["EventTime"] = _clock.GetCurrentInstant().ToString()
        };
        
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                logProperties[$"Property_{prop.Key}"] = prop.Value;
            }
        }
        
        if (metrics != null)
        {
            foreach (var metric in metrics)
            {
                logProperties[$"Metric_{metric.Key}"] = metric.Value;
            }
        }
        
        _logger.LogWithContext(LogLevel.Information, $"Event tracked: {eventName}", logProperties);
    }
    
    public void TrackAvailability(string name, bool isAvailable, Duration? duration = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["ServiceName"] = name,
            ["IsAvailable"] = isAvailable,
            ["CheckTime"] = _clock.GetCurrentInstant().ToString()
        };
        
        if (duration.HasValue)
        {
            properties["DurationMs"] = duration.Value.TotalMilliseconds;
        }
        
        _logger.LogWithContext(
            isAvailable ? LogLevel.Information : LogLevel.Warning,
            $"Availability check: {name} is {(isAvailable ? "available" : "unavailable")}",
            properties);
    }
    
    private static string BuildMetricKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return name;
        }
        
        var tagString = string.Join(",", tags.OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}"));
        return $"{name}[{tagString}]";
    }
    
    private class DurationMeasurement : IDurationMeasurement
    {
        private readonly InMemoryMetricsPortal _portal;
        private readonly string _name;
        private readonly Dictionary<string, string> _tags;
        private readonly Stopwatch _stopwatch;
        private readonly IClock _clock;
        private bool _success = true;
        
        public DurationMeasurement(InMemoryMetricsPortal portal, string name, Dictionary<string, string>? tags, IClock clock)
        {
            _portal = portal;
            _name = name;
            _tags = tags ?? new Dictionary<string, string>();
            _clock = clock;
            _stopwatch = Stopwatch.StartNew();
        }
        
        public void AddTag(string key, string value)
        {
            _tags[key] = value;
        }
        
        public void SetSuccess(bool success)
        {
            _success = success;
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _tags["Success"] = _success.ToString();
            
            var duration = Duration.FromMilliseconds(_stopwatch.ElapsedMilliseconds);
            _portal.RecordDuration(_name, duration, _tags);
        }
    }
}