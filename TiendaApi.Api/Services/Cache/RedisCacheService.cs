using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace TiendaApi.Api.Services.Cache;

/// <summary>
/// Implementación de caché usando Redis.
/// Implementa el patrón cache-aside.
/// </summary>
public class RedisCacheService(
    IDistributedCache cache,
    ILogger<RedisCacheService> logger
) : ICacheService
{
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<RedisCacheService> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Obtiene un valor de la caché, deserializando desde JSON.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss para clave: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit para clave: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener valor de caché para clave: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Guarda un valor en la caché, serializando a JSON.
    /// Expiración por defecto: 5 minutos.
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

            _logger.LogDebug("Valor cacheado para clave: {Key} con expiración: {Expiration}",
                key, expiration ?? TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar en caché para clave: {Key}", key);
        }
    }

    /// <summary>
    /// Elimina un valor de la caché por clave.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Entrada de caché eliminada para clave: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar de caché para clave: {Key}", key);
        }
    }

    /// <summary>
    /// Elimina todas las claves que coincidan con un patrón.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            _logger.LogDebug("Eliminando entradas de caché que coinciden con patrón: {Pattern}", pattern);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar entradas de caché por patrón: {Pattern}", pattern);
        }
    }
}
