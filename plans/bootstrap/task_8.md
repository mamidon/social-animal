Implement portal infrastructure adapters

This task creates the infrastructure implementations of the portal interfaces defined in Core, providing in-memory and console-based implementations that can be easily swapped for cloud services in the future following the Hexagonal Architecture pattern.

## Work to be Done

### Console Logger Portal Implementation
Create `ConsoleLoggerPortal.cs` in `SocialAnimal.Infrastructure/Portals/ConsoleLoggerPortal.cs`:

```csharp
using System.Text.Json;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Portals;

public class ConsoleLoggerPortal : ILoggerPortal
{
    private readonly string _componentName;
    private readonly Dictionary<string, object> _globalProperties;
    
    public ConsoleLoggerPortal(string componentName)
    {
        _componentName = componentName;
        _globalProperties = new Dictionary<string, object>
        {
            ["Component"] = componentName,
            ["MachineName"] = Environment.MachineName
        };
    }
    
    public void LogDebug(string message, params object[] args)
    {
        Log(LogLevel.Debug, message, args);
    }
    
    public void LogInformation(string message, params object[] args)
    {
        Log(LogLevel.Information, message, args);
    }
    
    public void LogWarning(string message, params object[] args)
    {
        Log(LogLevel.Warning, message, args);
    }
    
    public void LogError(Exception exception, string message, params object[] args)
    {
        var properties = new Dictionary<string, object>
        {
            ["Exception"] = exception.ToString(),
            ["ExceptionType"] = exception.GetType().Name,
            ["StackTrace"] = exception.StackTrace ?? string.Empty
        };
        
        Log(LogLevel.Error, message, args, properties);
    }
    
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        var properties = new Dictionary<string, object>
        {
            ["Exception"] = exception.ToString(),
            ["ExceptionType"] = exception.GetType().Name,
            ["StackTrace"] = exception.StackTrace ?? string.Empty
        };
        
        Log(LogLevel.Critical, message, args, properties);
    }
    
    public void LogWithContext(LogLevel level, string message, Dictionary<string, object> properties)
    {
        Log(level, message, Array.Empty<object>(), properties);
    }
    
    public IDisposable BeginScope(string name, Dictionary<string, object> properties)
    {
        return new LogScope(this, name, properties);
    }
    
    private void Log(LogLevel level, string message, object[] args, Dictionary<string, object>? additionalProperties = null)
    {
        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        
        var logEntry = new Dictionary<string, object>(_globalProperties)
        {
            ["Timestamp"] = DateTime.UtcNow.ToString("O"),
            ["Level"] = level.ToString(),
            ["Message"] = formattedMessage
        };
        
        if (additionalProperties != null)
        {
            foreach (var prop in additionalProperties)
            {
                logEntry[prop.Key] = prop.Value;
            }
        }
        
        var json = JsonSerializer.Serialize(logEntry);
        
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = GetColorForLevel(level);
        Console.WriteLine(json);
        Console.ForegroundColor = originalColor;
    }
    
    private static ConsoleColor GetColorForLevel(LogLevel level) => level switch
    {
        LogLevel.Debug => ConsoleColor.Gray,
        LogLevel.Information => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };
    
    private class LogScope : IDisposable
    {
        private readonly ConsoleLoggerPortal _logger;
        
        public LogScope(ConsoleLoggerPortal logger, string name, Dictionary<string, object> properties)
        {
            _logger = logger;
            _logger.LogDebug($"Entering scope: {name}", properties);
        }
        
        public void Dispose()
        {
            // Scope cleanup if needed
        }
    }
}
```

### In-Memory Message Portal Implementation
Create `InMemoryMessagePortal.cs` in `SocialAnimal.Infrastructure/Portals/InMemoryMessagePortal.cs`:

```csharp
using System.Collections.Concurrent;
using NodaTime;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Portals;

public class InMemoryMessagePortal : IMessagePublisher, IMessageDispatcher
{
    private readonly ConcurrentDictionary<Type, List<object>> _subscribers;
    private readonly ConcurrentQueue<QueuedMessage> _messageQueue;
    private readonly IClock _clock;
    private readonly ILoggerPortal _logger;
    private CancellationTokenSource? _processingCts;
    private Task? _processingTask;
    
    public InMemoryMessagePortal(IClock clock, ILoggerPortal logger)
    {
        _subscribers = new ConcurrentDictionary<Type, List<object>>();
        _messageQueue = new ConcurrentQueue<QueuedMessage>();
        _clock = clock;
        _logger = logger;
    }
    
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        await PublishAsync(message, _clock.GetCurrentInstant(), cancellationToken);
    }
    
    public async Task PublishAsync<TMessage>(TMessage message, Instant scheduledTime, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        var queuedMessage = new QueuedMessage
        {
            Message = message,
            MessageType = typeof(TMessage),
            ScheduledTime = scheduledTime,
            EnqueuedTime = _clock.GetCurrentInstant()
        };
        
        _messageQueue.Enqueue(queuedMessage);
        
        _logger.LogDebug("Message enqueued: {MessageType} scheduled for {ScheduledTime}", 
            typeof(TMessage).Name, scheduledTime);
        
        await Task.CompletedTask;
    }
    
    public async Task PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        foreach (var message in messages)
        {
            await PublishAsync(message, cancellationToken);
        }
    }
    
    public void RegisterSubscriber<TMessage>(IMessageSubscriber<TMessage> subscriber) where TMessage : class
    {
        var messageType = typeof(TMessage);
        _subscribers.AddOrUpdate(messageType,
            new List<object> { subscriber },
            (_, list) =>
            {
                list.Add(subscriber);
                return list;
            });
        
        _logger.LogInformation("Subscriber registered for message type: {MessageType}", messageType.Name);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = ProcessMessagesAsync(_processingCts.Token);
        
        _logger.LogInformation("Message dispatcher started");
        await Task.CompletedTask;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _processingCts?.Cancel();
        
        if (_processingTask != null)
        {
            await _processingTask;
        }
        
        _logger.LogInformation("Message dispatcher stopped");
    }
    
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_messageQueue.TryDequeue(out var queuedMessage))
                {
                    var now = _clock.GetCurrentInstant();
                    
                    if (queuedMessage.ScheduledTime <= now)
                    {
                        await ProcessMessage(queuedMessage, cancellationToken);
                    }
                    else
                    {
                        // Re-queue for later processing
                        _messageQueue.Enqueue(queuedMessage);
                        await Task.Delay(100, cancellationToken);
                    }
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message queue");
            }
        }
    }
    
    private async Task ProcessMessage(QueuedMessage queuedMessage, CancellationToken cancellationToken)
    {
        if (_subscribers.TryGetValue(queuedMessage.MessageType, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    var handleMethod = subscriber.GetType()
                        .GetMethod("HandleAsync", new[] { queuedMessage.MessageType, typeof(CancellationToken) });
                    
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(subscriber, new[] { queuedMessage.Message, cancellationToken })!;
                        await task;
                    }
                    
                    _logger.LogDebug("Message processed: {MessageType}", queuedMessage.MessageType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {MessageType}", queuedMessage.MessageType.Name);
                    
                    var errorMethod = subscriber.GetType()
                        .GetMethod("OnErrorAsync", new[] { queuedMessage.MessageType, typeof(Exception), typeof(CancellationToken) });
                    
                    if (errorMethod != null)
                    {
                        var task = (Task)errorMethod.Invoke(subscriber, new[] { queuedMessage.Message, ex, cancellationToken })!;
                        await task;
                    }
                }
            }
        }
    }
    
    private class QueuedMessage
    {
        public required object Message { get; init; }
        public required Type MessageType { get; init; }
        public required Instant ScheduledTime { get; init; }
        public required Instant EnqueuedTime { get; init; }
    }
}
```

### In-Memory Metrics Portal Implementation
Create `InMemoryMetricsPortal.cs` in `SocialAnimal.Infrastructure/Portals/InMemoryMetricsPortal.cs`:

```csharp
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
```

### System Clock Portal Implementation
Create `SystemClockPortal.cs` in `SocialAnimal.Infrastructure/Portals/SystemClockPortal.cs`:

```csharp
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
```

### Configuration Portal Implementation
Create `ConfigurationPortal.cs` in `SocialAnimal.Infrastructure/Portals/ConfigurationPortal.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Portals;

public class ConfigurationPortal : IConfigurationPortal
{
    private readonly IConfiguration _configuration;
    private readonly string _environmentName;
    
    public ConfigurationPortal(IConfiguration configuration, string environmentName)
    {
        _configuration = configuration;
        _environmentName = environmentName;
    }
    
    public string EnvironmentName => _environmentName;
    
    public T GetConfiguration<T>(string sectionName) where T : class, new()
    {
        var config = new T();
        _configuration.GetSection(sectionName).Bind(config);
        return config;
    }
    
    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name) 
            ?? throw new InvalidOperationException($"Connection string '{name}' not found");
    }
    
    public string GetValue(string key)
    {
        return _configuration[key] 
            ?? throw new InvalidOperationException($"Configuration key '{key}' not found");
    }
    
    public bool GetBool(string key, bool defaultValue = false)
    {
        var value = _configuration[key];
        return value != null ? bool.Parse(value) : defaultValue;
    }
    
    public int GetInt(string key, int defaultValue = 0)
    {
        var value = _configuration[key];
        return value != null ? int.Parse(value) : defaultValue;
    }
    
    public bool IsDevelopment()
    {
        return _environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase);
    }
    
    public bool IsProduction()
    {
        return _environmentName.Equals("Production", StringComparison.OrdinalIgnoreCase);
    }
}
```

## Relevant Patterns from CLAUDE.md

- **Clean Architecture / Hexagonal Architecture**: Infrastructure implementations of Core portal interfaces
- **Portals Pattern**: Interfaces in Core/Portals with implementations in Infrastructure
- **Message-Driven Architecture**: In-memory message queue with publisher/subscriber pattern
- **Time and Date Handling**: NodaTime integration throughout portal implementations
- **Configuration Management**: Type-safe configuration access through portal abstraction

## Deliverables

1. `ConsoleLoggerPortal.cs` - Structured console logging implementation
2. `InMemoryMessagePortal.cs` - In-memory message queue with scheduling
3. `InMemoryMetricsPortal.cs` - In-memory metrics collection
4. `SystemClockPortal.cs` - NodaTime-based clock implementation
5. `ConfigurationPortal.cs` - Type-safe configuration wrapper

## Acceptance Criteria

- All portal implementations compile without errors
- Console logger outputs structured JSON logs with appropriate colors
- Message portal supports immediate and scheduled message delivery
- Metrics portal tracks counters, gauges, and durations
- Clock portal properly uses NodaTime for all time operations
- Configuration portal provides type-safe access to settings
- All implementations can be easily swapped for cloud services
- Proper async/await patterns are used throughout
- Error handling and logging are comprehensive