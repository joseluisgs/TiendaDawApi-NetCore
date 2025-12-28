namespace TiendaApi.Services.Cache;

/// <summary>
/// Interface for distributed caching service
/// Java Spring equivalent: CacheManager or RedisTemplate
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get value from cache by key
    /// </summary>
    Task<T?> GetAsync<T>(string key);
    
    /// <summary>
    /// Set value in cache with expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Remove value from cache by key
    /// </summary>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Remove all keys matching pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern);
}
