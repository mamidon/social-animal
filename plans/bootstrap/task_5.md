Create portal interfaces for hexagonal architecture

This task establishes the portal interfaces in the Core project that define contracts for external dependencies. Following the Hexagonal Architecture pattern from CLAUDE.md, these interfaces allow the core domain to remain independent of infrastructure implementations.

## Work to be Done

### Logger Portal Interface
Create `ILoggerPortal` in `SocialAnimal.Core/Portals/ILoggerPortal.cs` for structured logging without infrastructure dependency:

```csharp
namespace SocialAnimal.Core.Portals;

public interface ILoggerPortal
{
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogCritical(Exception exception, string message, params object[] args);
    
    // Structured logging with properties
    void LogWithContext(LogLevel level, string message, Dictionary<string, object> properties);
    
    // Scoped logging for correlation
    IDisposable BeginScope(string name, Dictionary<string, object> properties);
}

public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
```

This portal abstracts logging infrastructure, allowing swapping between console logging, Application Insights, or CloudWatch without changing core logic.

### Message Portal Interfaces
Create messaging interfaces in `SocialAnimal.Core/Portals/IMessagePortal.cs` following the Message-Driven Architecture pattern:

```csharp
using NodaTime;

namespace SocialAnimal.Core.Portals;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class;
    
    Task PublishAsync<TMessage>(TMessage message, Instant scheduledTime, CancellationToken cancellationToken = default) 
        where TMessage : class;
    
    Task PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default) 
        where TMessage : class;
}

public interface IMessageSubscriber<TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
    Task OnErrorAsync(TMessage message, Exception exception, CancellationToken cancellationToken = default);
}

public interface IMessageDispatcher
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void RegisterSubscriber<TMessage>(IMessageSubscriber<TMessage> subscriber) where TMessage : class;
}
```

These interfaces enable:
- Publishing messages asynchronously
- Scheduled message delivery
- Batch message publishing
- Type-safe message subscription
- Error handling in message processing

### Metrics Portal Interface
Create `IMetricsPortal` in `SocialAnimal.Core/Portals/IMetricsPortal.cs` for performance monitoring:

```csharp
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
```

This portal abstracts metrics collection, supporting future migration to Prometheus, Azure Monitor, or CloudWatch.

### Clock Portal Interface
Create `IClockPortal` in `SocialAnimal.Core/Portals/IClockPortal.cs` for time abstraction using NodaTime:

```csharp
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
```

This provides testable time operations and consistent time handling across the application.

### Configuration Portal Interface
Create `IConfigurationPortal` in `SocialAnimal.Core/Portals/IConfigurationPortal.cs` for type-safe configuration access:

```csharp
namespace SocialAnimal.Core.Portals;

public interface IConfigurationPortal
{
    T GetConfiguration<T>(string sectionName) where T : class, new();
    string GetConnectionString(string name);
    string GetValue(string key);
    bool GetBool(string key, bool defaultValue = false);
    int GetInt(string key, int defaultValue = 0);
    
    // Environment detection
    bool IsDevelopment();
    bool IsProduction();
    string EnvironmentName { get; }
}
```

### Cache Portal Interface
Create `ICachePortal` in `SocialAnimal.Core/Portals/ICachePortal.cs` for caching abstraction:

```csharp
using NodaTime;

namespace SocialAnimal.Core.Portals;

public interface ICachePortal
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, Duration? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    // Atomic operations
    Task<long> IncrementAsync(string key, long value = 1, Duration? expiration = null, CancellationToken cancellationToken = default);
    Task<bool> SetIfNotExistsAsync<T>(string key, T value, Duration? expiration = null, CancellationToken cancellationToken = default) where T : class;
}
```

## Relevant Patterns from CLAUDE.md

- **Clean Architecture / Hexagonal Architecture**: Portals Pattern for defining contracts between layers
- **Dependency Direction**: Core defines interfaces that Infrastructure implements
- **Message-Driven Architecture**: IMessagePublisher and IMessageSubscriber interfaces for async processing
- **Time and Date Handling**: Using NodaTime types in portal interfaces
- **Configuration Management**: Type-safe configuration through portal abstraction

## Deliverables

1. `ILoggerPortal.cs` - Structured logging interface
2. `IMessagePortal.cs` - Message publishing and subscription interfaces
3. `IMetricsPortal.cs` - Performance monitoring and metrics collection
4. `IClockPortal.cs` - Time abstraction using NodaTime
5. `IConfigurationPortal.cs` - Type-safe configuration access
6. `ICachePortal.cs` - Caching abstraction interface

## Acceptance Criteria

- All portal interfaces compile without errors
- Interfaces use NodaTime types for time-related operations
- No infrastructure dependencies in interface definitions
- Support for structured logging with context and correlation
- Message interfaces support async processing and error handling
- Metrics interface supports common telemetry patterns
- Configuration interface provides type-safe access
- All interfaces follow the Portals Pattern from CLAUDE.md
- Interfaces are designed for easy swapping of implementations