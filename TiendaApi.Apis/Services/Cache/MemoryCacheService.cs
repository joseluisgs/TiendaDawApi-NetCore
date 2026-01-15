using Microsoft.Extensions.Caching.Memory;

namespace TiendaApi.Apis.Services.Cache;

/// <summary>
/// Implementación en memoria de ICacheService para desarrollo local.
/// No requiere Redis ni servicios externos.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = _cache.Get<T>(key);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting from cache: Key={Key}", key);
            return Task.FromResult(default(T));
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            }

            _cache.Set(key, value, options);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache: Key={Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from cache: Key={Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // MemoryCache no soporta RemoveByPattern nativamente
            // Para desarrollo local, esto es aceptable
            _logger.LogDebug("RemoveByPattern not fully supported in MemoryCache. Pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing by pattern from cache: Pattern={Pattern}", pattern);
            return Task.CompletedTask;
        }
    }
}
