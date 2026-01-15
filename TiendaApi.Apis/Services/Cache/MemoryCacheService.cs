using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TiendaApi.Apis.Services.Cache;

/// <summary>
/// Implementación concreta de <see cref="ICacheService"/> que utiliza el mecanismo de caché en memoria
/// de ASP.NET Core (<see cref="IMemoryCache"/>).
/// 
/// <para>Esta implementación está diseñada específicamente para entornos de desarrollo local y pruebas,
/// ya que no requiere configuración de servicios externos como Redis.</para>
/// 
/// <remarks>
/// <para><b>Características principales:</b></para>
/// <list type="bullet">
///   <item><description>Almacenamiento en memoria del proceso de la aplicación.</description></item>
///   <item><description>Tiempo de acceso extremadamente rápido (microsegundos).</description></item>
///   <item><description>No requiere configuración adicional de infraestructura.</description></item>
///   <item><description>Persistencia limitada al ciclo de vida del proceso.</description></item>
/// </list>
/// 
/// <para><b>Limitaciones respecto a caché distribuida:</b></para>
/// <list type="number">
///   <item><description>No se comparte entre múltiples instancias de la aplicación.</description></item>
///   <item><description>Se pierde al reiniciar la aplicación o el servidor.</description></item>
///   <item><description>La operación <see cref="RemoveByPatternAsync"/> tiene soporte limitado.</description></item>
///   <item><description>Consume memoria del proceso principal.</description></item>
/// </list>
/// 
/// <para><b>Casos de uso apropiados:</b></para>
/// <list type="bullet">
///   <item><description>Desarrollo y depuración de la aplicación.</description></item>
///   <item><description>Pruebas unitarias y de integración.</description></item>
///   <item><description>Entornos de demostración sin conectividad a Redis.</description></item>
///   <item><description>Caché de datos altamente volátiles.</description></item>
/// </list>
/// 
/// <para><b>Configuración recomendada en desarrollo:</b></para>
/// <code>
/// services.AddMemoryCache();
/// services.AddSingleton&lt;ICacheService, MemoryCacheService&gt;();
/// </code>
/// </remarks>
/// 
/// <example>
/// <para>Inyección en el constructor de un controlador:</para>
/// <code>
/// public class ProductoController : ControllerBase
/// {
///     private readonly ICacheService _cacheService;
///     
///     public ProductoController(ICacheService cacheService)
///     {
///         _cacheService = cacheService;
///     }
/// }
/// </code>
/// </example>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    /// <summary>
    /// Constructor que inicializa el servicio de caché en memoria.
    /// </summary>
    /// <param name="cache">Instancia de <see cref="IMemoryCache"/> proporcionada por el contenedor de dependencias.</param>
    /// <param name="logger">Instancia de logger para registrar operaciones y errores.</param>
    /// 
    /// <remarks>
    /// <para><b>Inicialización:</b> El parámetro <paramref name="cache"/> debe estar configurado
    /// previamente en el contenedor de servicios mediante <c>services.AddMemoryCache()</c>.</para>
    /// 
    /// <para><b>Logger:</b> Se utiliza para registrar advertencias en lugar de excepciones,
    /// permitiendo que la aplicación continúe funcionando incluso si la caché falla.</para>
    /// </remarks>
    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Recupera un valor de la caché en memoria de forma asíncrona.
    /// Utiliza <c>IMemoryCache.Get</c> internamente para obtener el valor.
    /// </summary>
    /// <typeparam name="T">Tipo del valor almacenado en caché.</typeparam>
    /// <param name="key">Clave única que identifica el valor en la caché.</param>
    /// <returns>
    /// Tarea asíncrona que contiene el valor si existe, o el valor predeterminado de T si no se encuentra.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Mecanismo interno:</b></para>
    /// <list type="bullet">
    ///   <item><description>Utiliza el método genérico <c>IMemoryCache.Get&lt;T&gt;()</c>.</description></item>
    ///   <item><description>Captura cualquier excepción y la registra como advertencia.</description></item>
    ///   <item><description>Devuelve <c>default(T)</c> en caso de error sin propagar la excepción.</description></item>
    /// </list>
    /// 
    /// <para><b>Excepciones manejadas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Errores de serialización del valor.</description></item>
///       <item><description>Problemas de memoria insuficiente.</description></item>
///       <item><description>Errores de acceso concurrente.</description></item>
///     </list>
///   </remarks>
/// 
///   <example>
///   <code>
///   var clave = $"usuario_{userId}";
///   var usuario = await _cacheService.GetAsync&lt;Usuario&gt;(clave);
///   
///   if (usuario == null)
///   {
///       // No está en caché, cargar desde base de datos
///       usuario = await _repo.ObtenerUsuarioAsync(userId);
///   }
///   </code>
///   </example>
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

    /// <summary>
    /// Almacena un valor en la caché en memoria con opciones de expiración.
    /// </summary>
    /// <typeparam name="T">Tipo del valor a almacenar.</typeparam>
    /// <param name="key">Clave única para identificar el valor.</param>
    /// <param name="value">Valor a almacenar en la caché.</param>
    /// <param name="expiration">
    /// Tiempo de expiración absoluto. Si es null, se utiliza un valor predeterminado de 5 minutos.
    /// </param>
    /// <returns>Tarea asíncrona que se completa al finalizar la operación.</returns>
    /// 
    /// <remarks>
    /// <para><b>Configuración de expiración:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si no se especifica <paramref name="expiration"/>, el valor predeterminado es 5 minutos.</description></item>
    ///   <item><description>La expiración es absoluta, no deslizante.</description></item>
    /// </list>
    /// 
    /// <para><b>Comportamiento en caso de error:</b></para>
    /// <list type="bullet">
    ///   <item><description>Las excepciones se capturan y registran como advertencias.</description></item>
    ///   <item><description>La operación se considera exitosa incluso si falla el almacenamiento.</description></item>
    ///   <item><description>La aplicación continúa sin interrupción.</description></item>
    /// </list>
    /// 
    /// <para><b>Valor predeterminado de expiración:</b></para>
    /// El tiempo de expiración predeterminado de 5 minutos es apropiado para datos que cambian
    /// frecuentemente pero no requieren actualización en tiempo real.
///   </remarks>
/// 
///   <example>
///   <code>
///   // Almacenar con expiración personalizada
///   await _cacheService.SetAsync("sesion_activa", datosSesion, TimeSpan.FromMinutes(30));
///   
///   // Almacenar con expiración predeterminada (5 minutos)
///   await _cacheService.SetAsync("contadores_dashboard", contadores);
///   </code>
///   </example>
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

    /// <summary>
    /// Elimina un valor específico de la caché en memoria.
    /// </summary>
    /// <param name="key">Clave única del valor a eliminar.</param>
    /// <returns>Tarea asíncrona que se completa después de la eliminación.</returns>
    /// 
    /// <remarks>
    /// <para><b>Mecanismo:</b> Utiliza el método <c>IMemoryCache.Remove()</c> que elimina
    /// la entrada de caché de forma inmediata.</para>
    /// 
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si la clave no existe, no se produce ningún error.</description></item>
    ///   <item><description>La operación se completa incluso si la clave no existía.</description></item>
    ///   <item><description>Las excepciones se capturan y registran como advertencias.</description></item>
    /// </list>
    /// 
    /// <example>
    /// <code>
    /// // Invalidar caché después de actualizar datos
    /// await _cacheService.RemoveAsync($"producto_{producto.Id}");
    /// </code>
    /// </example>
    /// </remarks>
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

    /// <summary>
    /// Elimina entradas de caché que coincidan con un patrón especificado.
    /// 
    /// <para><b>Nota importante:</b> Esta implementación tiene soporte limitado porque
    /// <see cref="IMemoryCache"/> no proporciona un método nativo para eliminar por patrón.</para>
    /// </summary>
    /// <param name="pattern">Patrón de búsqueda para las claves a eliminar.</param>
    /// <returns>Tarea asíncrona que se completa después del procesamiento.</returns>
    /// 
    /// <remarks>
    /// <para><b>Limitaciones actuales:</b></para>
    /// <list type="bullet">
    ///   <item><description>La implementación solo registra el patrón en el log de depuración.</description></item>
    ///   <item><description>No realiza ninguna eliminación real de entradas.</description></item>
    ///   <item><description>Esta limitación es aceptable para entornos de desarrollo.</description></item>
    /// </list>
    /// 
    /// <para><b>Solución alternativa para producción:</b></para>
    /// En un entorno de producción con caché distribuida (Redis), implemente esta operación
    /// utilizando los comandos nativos de Redis como SCAN con patrón o KEYS con wildcard.
    /// 
    /// <para><b>Recomendación:</b> Para invalidar entradas específicas en desarrollo,
    /// utilice <see cref="RemoveAsync"/> con las claves conocidas.</para>
    /// 
    /// <example>
    /// <code>
    /// // Esta operación registra el patrón pero no elimina entradas
    /// await _cacheService.RemoveByPatternAsync("productos_categoria_*");
    /// 
    /// // Alternativa: eliminar claves específicas conocidas
    /// for (int i = 0; i < 100; i++)
    /// {
    ///     await _cacheService.RemoveAsync($"productos_categoria_{categoriaId}_{i}");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <exception cref="NotImplementedException">
    /// Esta implementación no lanza excepción; simplemente registra un warning en el log.
    /// </exception>
    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            _logger.LogDebug(
                "RemoveByPattern no está completamente soportado en MemoryCache. " +
                "En producción, utilice un proveedor de caché distribuida como Redis. " +
                "Patrón solicitado: {Pattern}", 
                pattern
            );
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error eliminando por patrón de caché. Patrón={Pattern}", pattern);
            return Task.CompletedTask;
        }
    }
}
