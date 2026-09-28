using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace TiendaApi.Api.Services.Cache;

/// <summary>
/// Implementación de ICacheService usando IMemoryCache para desarrollo local.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    /// <summary>
    /// Crea una instancia del servicio de caché en memoria.
    /// </summary>
    /// <param name="cache">Caché en memoria.</param>
    /// <param name="logger">Logger de la instancia.</param>
    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = _cache.Get<T>(key);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo de caché. Clave={Key}", key);
            return Task.FromResult(default(T));
        }
    }

    /// <inheritdoc />
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
            _logger.LogWarning(ex, "Error estableciendo caché. Clave={Key}", key);
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error eliminando de caché. Clave={Key}", key);
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            _logger.LogDebug(
                "RemoveByPattern no soportado en MemoryCache. " +
                "En producción, use Redis. Patrón: {Pattern}",
                pattern
            );
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error eliminando por patrón. Patrón={Pattern}", pattern);
            return Task.CompletedTask;
        }
    }
}
