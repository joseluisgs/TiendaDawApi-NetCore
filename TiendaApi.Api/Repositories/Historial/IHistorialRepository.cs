using TiendaApi.Api.Dtos.Historial;

namespace TiendaApi.Api.Repositories.Historial;

/// <summary>
/// Acceso a las series temporales de Cassandra (histórico de precios y feed de
/// actividad). Cada método responde a UNA consulta planificada (query-first):
/// las tablas existen porque la pizarra de consultas existía antes.
/// </summary>
public interface IHistorialRepository
{
    /// <summary>
    /// Q-A: histórico de precios de un producto. Lee la partición ENTERA del
    /// producto; el orden (más reciente primero) lo da la clustering key, sin ORDER BY.
    /// </summary>
    /// <param name="productoId">Producto (partition key).</param>
    /// <param name="desde">Fecha mínima opcional (rango sobre clustering key: permitido).</param>
    /// <param name="hasta">Fecha máxima opcional.</param>
    /// <param name="limit">Máximo de puntos a devolver (LIMIT).</param>
    /// <returns>Puntos del histórico, del más reciente al más antiguo.</returns>
    Task<IReadOnlyList<HistorialPrecioDto>> GetHistorialPreciosAsync(
        long productoId, DateTime? desde, DateTime? hasta, int limit);

    /// <summary>
    /// Q-B: feed de actividad — últimos productos que vio un usuario (LIMIT sobre
    /// la partición; la tabla aplica TTL de 180 días de forma nativa).
    /// </summary>
    /// <param name="usuarioId">Usuario (partition key).</param>
    /// <param name="limit">Tamaño del feed (la API usa 20).</param>
    /// <returns>Entradas del feed, de la más reciente a la más antigua.</returns>
    Task<IReadOnlyList<VistaFeedDto>> GetFeedVistasAsync(long usuarioId, int limit);

    /// <summary>
    /// Q-C: cambios de precio en un rango de días (informes). Como un rango sobre
    /// partition key NO está permitido en CQL, se resuelve con IN: lookup puntual
    /// de hasta 31 particiones (una por día).
    /// </summary>
    /// <param name="desde">Primer día incluido.</param>
    /// <param name="hasta">Último día incluido (máximo 31 días de margen).</param>
    /// <returns>Cambios ordenados por día y producto.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El rango supera los 31 días.</exception>
    Task<IReadOnlyList<CambioPrecioDto>> GetCambiosPrecioAsync(DateOnly desde, DateOnly hasta);

    /// <summary>
    /// Escritura de precio: añade un punto al histórico (Q-A) Y al informe del
    /// día (Q-C). DOS tablas, DOS inserts: Cassandra no tiene transacciones
    /// repartidas, la atomicidad es responsabilidad de la aplicación.
    /// </summary>
    /// <param name="productoId">Producto.</param>
    /// <param name="precio">Precio nuevo.</param>
    /// <returns>Tarea asíncrona.</returns>
    Task RegistrarPrecioAsync(long productoId, double precio);

    /// <summary>
    /// Escritura de actividad (Q-B): registra la vista de un producto
    /// denormalizando su nombre. El TTL lo aplica la tabla (default_time_to_live).
    /// </summary>
    /// <param name="usuarioId">Usuario que ve el producto (partition key).</param>
    /// <param name="productoId">Producto visto.</param>
    /// <param name="nombre">Nombre del producto, copiado a la fila.</param>
    /// <returns>Tarea asíncrona.</returns>
    Task RegistrarVistaAsync(long usuarioId, long productoId, string nombre);
}
