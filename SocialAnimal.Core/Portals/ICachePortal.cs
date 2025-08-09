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