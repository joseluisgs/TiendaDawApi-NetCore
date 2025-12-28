using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace TiendaApi.Services.Cache;

/// <summary>
/// Redis cache service implementation using IDistributedCache
/// Java Spring equivalent: RedisTemplate or CacheManager implementation
/// Implements cache-aside pattern
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Get value from cache, deserializing from JSON
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Set value in cache, serializing to JSON
    /// Default expiration from configuration or 5 minutes
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            await _cache.SetStringAsync(key, jsonValue, options);
            
            _logger.LogDebug("Value cached for key: {Key} with expiration: {Expiration}", 
                key, expiration ?? TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
            // Don't throw - cache failures should not break the application
        }
    }

    /// <summary>
    /// Remove value from cache by key
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Cache entry removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Remove all keys matching pattern
    /// Note: This is a simplified implementation. For production, consider using Redis SCAN
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            _logger.LogDebug("Removing cache entries matching pattern: {Pattern}", pattern);
            // Note: IDistributedCache doesn't support pattern removal directly
            // In production, you would use StackExchange.Redis directly for this
            // For now, we log it but don't implement complex pattern matching
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
        }
    }
}
