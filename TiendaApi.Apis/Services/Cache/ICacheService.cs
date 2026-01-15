using System;
using System.Threading.Tasks;

namespace TiendaApi.Apis.Services.Cache;

/// <summary>
/// Interfaz que define el contrato para un servicio de caché distribuida.
/// Proporciona operaciones básicas de almacenamiento en caché para optimizar el rendimiento
/// de la aplicación mediante la reducción de consultas costosas a la base de datos.
///
/// <para>Esta interfaz está diseñada para ser implementada por diferentes proveedores de caché,
/// permitiendo una abstracción sobre el mecanismo de almacenamiento subyacente.</para>
/// 
/// <remarks>
/// <para><b>Patrón de uso recomendado:</b></para>
/// <list type="number">
///   <item><description>Utilice <see cref="GetAsync{T}"/> para recuperar datos en caché antes de consultar la base de datos.</description></item>
///   <item><description>Utilice <see cref="SetAsync{T}"/> para almacenar resultados de operaciones costosas.</description></item>
///   <item><description>Utilice <see cref="RemoveAsync"/> para invalidar entradas específicas cuando los datos cambian.</description></item>
///   <item><description>Utilice <see cref="RemoveByPatternAsync"/> para invalidar múltiples entradas que coincidan con un patrón.</description></item>
/// </list>
/// 
/// <para><b>Consideraciones de rendimiento:</b></para>
/// <list type="bullet">
///   <item><description>Los valores en caché deben serializarse correctamente.</description></item>
///   <item><description>Establezca tiempos de expiración apropiados según la naturaleza de los datos.</description></item>
///   <item><description>Evite almacenar información sensible sin cifrado adicional.</description></item>
/// </list>
/// </remarks>
/// 
/// <example>
/// <para>Ejemplo de uso básico para caché de productos:</para>
/// <code>
/// // Obtener productos de la caché
/// var cacheKey = $"productos_categoria_{categoriaId}";
/// var productos = await _cacheService.GetAsync&lt;List&lt;Producto&gt;&gt;(cacheKey);
///
/// if (productos == null)
/// {
///     // Si no está en caché, consultar base de datos
///     productos = await _productoRepository.ObtenerPorCategoriaAsync(categoriaId);
///     
///     // Guardar en caché con expiración de 10 minutos
///     await _cacheService.SetAsync(cacheKey, productos, TimeSpan.FromMinutes(10));
/// }
/// </code>
/// </example>
public interface ICacheService
{
    /// <summary>
    /// Recupera un valor almacenado en la caché mediante su clave única.
    /// Si la clave no existe o ha expirado, devuelve el valor predeterminado del tipo.
    /// </summary>
    /// <typeparam name="T">Tipo genérico del valor almacenado. Debe ser serializable.</typeparam>
    /// <param name="key">Clave única que identifica el valor en la caché. No debe ser null ni estar vacía.</param>
    /// <returns>
    /// Tarea asíncrona que contiene el valor almacenado si existe, o el valor predeterminado de T
    /// si la clave no se encuentra. Devuelve null para tipos de referencia cuando no existe.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item><description>Esta operación es de solo lectura y no modifica el estado de la caché.</description></item>
    ///   <item><description>Si la clave no existe, se devuelve el valor predeterminado sin lanzar excepción.</description></item>
    ///   <item><description>El tiempo de ejecución depende del proveedor de caché subyacente.</description></item>
    /// </list>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Recuperar datos de usuario de la caché
    /// var cacheKey = $"usuario_{userId}";
    /// var usuario = await _cacheService.GetAsync&lt;Usuario&gt;(cacheKey);
    /// 
    /// if (usuario != null)
    /// {
    ///     Console.WriteLine($"Usuario encontrado en caché: {usuario.Nombre}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Usuario no encontrado en caché, consultando base de datos...");
    /// }
    /// </code>
    /// </example>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Almacena un valor en la caché con una clave asociada y opcionalmente un tiempo de expiración.
    /// Si ya existe un valor para la clave especificada, será reemplazado por el nuevo valor.
    /// </summary>
    /// <typeparam name="T">Tipo genérico del valor a almacenar. Debe ser serializable.</typeparam>
    /// <param name="key">Clave única que identificará el valor en la caché.</param>
    /// <param name="value">Valor a almacenar en la caché. Puede ser null para tipos de referencia.</param>
    /// <param name="expiration">
    /// Tiempo opcional después del cual el valor será automáticamente eliminado de la caché.
    /// Si es null, se utilizará el tiempo de expiración predeterminado del proveedor.
    /// </param>
    /// <returns>
    /// Tarea asíncrona que se completa cuando el valor ha sido almacenado exitosamente.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Patrón de escritura:</b></para>
    /// <list type="number">
    ///   <item><description>Verificar si los datos ya existen en caché antes de escribir.</description></item>
    ///   <item><description>Establecer expiración basada en la frecuencia de actualización de los datos.</description></item>
    ///   <item><description>Considerar el tamaño del objeto para evitar consumir excesivamente la memoria.</description></item>
    /// </list>
    /// 
    /// <para><b>Ejemplos de tiempos de expiración:</b></para>
    /// <list type="bullet">
    ///   <item><description>Configuraciones de usuario: 30-60 minutos</description></item>
    ///   <item><description>Listas de productos: 5-15 minutos</description></item>
    ///   <item><description>Datos de catálogo: 1-24 horas</description></item>
    ///   <item><description>Sensiones de usuario: Duración de la sesión</description></item>
    /// </list>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Almacenar resultado de consulta con expiración de 5 minutos
    /// await _cacheService.SetAsync(
    ///     $"productos_dashboard", 
    ///     productos, 
    ///     TimeSpan.FromMinutes(5)
    /// );
    /// 
    /// // Almacenar con expiración predeterminada
    /// await _cacheService.SetAsync("configuracion_global", config);
    /// </code>
    /// </example>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Elimina un valor específico de la caché utilizando su clave única.
    /// Si la clave no existe, la operación se completa sin error.
    /// </summary>
    /// <param name="key">Clave única del valor a eliminar de la caché.</param>
    /// <returns>
    /// Tarea asíncrona que se completa cuando el valor ha sido eliminado o no existía.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Cuándo usar:</b></para>
    /// <list type="bullet">
    ///   <item><description>Cuando los datos subyacentes se actualizan o eliminan.</description></item>
    ///   <item><description>Cuando el usuario cierra sesión y necesita invalidar tokens en caché.</description></item>
    ///   <item><description>Después de operaciones CRUD que afectan entidades específicas.</description></item>
    /// </list>
    /// 
    /// <para><b>Nota:</b> Esta operación es idempotente; llamar múltiples veces con la misma
    /// clave no causa errores ni efectos secundarios adversos.</para>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Invalidar caché después de actualizar un producto
    /// await _productoService.ActualizarAsync(producto);
    /// await _cacheService.RemoveAsync($"producto_{producto.Id}");
    /// await _cacheService.RemoveAsync("productos_lista");
    /// </code>
    /// </example>
    Task RemoveAsync(string key);

    /// <summary>
    /// Elimina todas las entradas de la caché cuyas claves coincidan con un patrón especificado.
    /// Útil para invalidar múltiples entradas relacionadas simultáneamente.
    /// </summary>
    /// <param name="pattern">
    /// Patrón de búsqueda para las claves a eliminar.
    /// El formato del patrón depende del proveedor de caché subyacente.
    /// </param>
    /// <returns>
    /// Tarea asíncrona que se completa cuando todas las entradas coincidentes han sido procesadas.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Compatibilidad:</b></para>
    /// <list type="bullet">
    ///   <item><description>Proveedores como Redis soportan patrones con wildcards (*).</description></item>
    ///   <item><description>Proveedores en memoria pueden tener soporte limitado para esta operación.</description></item>
    /// </list>
    /// 
    /// <para><b>Ejemplos de patrones comunes:</b></para>
    /// <list type="bullet">
    ///   <item><description>"productos_*" - Elimina todas las claves que comienzan con "productos_"</description></item>
    ///   <item><description>"*_categoria_*" - Elimina claves que contengan "_categoria_" en cualquier posición</description></item>
    ///   <item><description>"usuario_123_*" - Elimina todas las entradas de un usuario específico</description></item>
    /// </list>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Invalidar todas las entradas de caché de un usuario específico
    /// await _cacheService.RemoveByPatternAsync($"usuario_{userId}_*");
    /// 
    /// // Invalidar caché de todos los productos de una categoría
    /// await _cacheService.RemoveByPatternAsync($"productos_categoria_{categoriaId}_*");
    /// 
    /// // Limpiar caché relacionada con configuración
    /// await _cacheService.RemoveByPatternAsync("config_*");
    /// </code>
    /// </example>
    Task RemoveByPatternAsync(string pattern);
}
