using System;

namespace TiendaApi.Api.Services.Cache;

/// <summary>
/// Contrato del servicio de caché.
/// </summary>
public interface ICacheService
{
    /// <summary>Obtiene un valor de la caché.</summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="key">Clave del valor.</param>
    /// <returns>Valor encontrado o default.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>Guarda un valor en la caché.</summary>
    /// <typeparam name="T">Tipo del valor.</typeparam>
    /// <param name="key">Clave.</param>
    /// <param name="value">Valor a guardar.</param>
    /// <param name="expiration">Tiempo de expiración.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>Elimina un valor de la caché.</summary>
    /// <param name="key">Clave a eliminar.</param>
    Task RemoveAsync(string key);

    /// <summary>Elimina valores por patrón.</summary>
    /// <param name="pattern">Patrón de búsqueda.</param>
    Task RemoveByPatternAsync(string pattern);
}
