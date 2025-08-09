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