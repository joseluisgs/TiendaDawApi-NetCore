namespace TiendaApi.Apis.Services.Cache;

/// <summary>
/// Interfaz para servicio de caché distribuida.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtiene un valor de la caché por clave.
    /// </summary>
    Task<T?> GetAsync<T>(string key);
    
    /// <summary>
    /// Guarda un valor en la caché con expiración.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Elimina un valor de la caché por clave.
    /// </summary>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Elimina todas las claves que coincidan con un patrón.
    /// </summary>
    Task RemoveByPatternAsync(string pattern);
}
