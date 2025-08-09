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