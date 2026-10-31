using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Historial;

/// <summary>
/// Fachada del histórico de precios y el feed de actividad (Cassandra).
///
/// 🎓 Reparto de responsabilidades al estilo de la casa:
/// - PostgreSQL (vía <c>IProductoRepository</c>) decide si el producto EXISTE:
///   es la fuente de verdad, y su 404 manda.
/// - Cassandra (vía <c>IHistorialRepository</c>) guarda las SERIES: dato derivado
///   que se consulta DESPUÉS de validar contra PostgreSQL, y cuya API trabaja
///   con listas — sin puntos = lista vacía, una respuesta válida como otra.
/// </summary>
public interface IHistorialService
{
    /// <summary>
    /// Serie histórica de precios de un producto (consulta Q-A).
    /// </summary>
    /// <param name="productoId">Producto cuya serie se consulta.</param>
    /// <param name="desde">Fecha mínima opcional (rango sobre clustering key).</param>
    /// <param name="hasta">Fecha máxima opcional.</param>
    /// <param name="limit">Puntos máximos (se acota a 1..200; por defecto pide 50).</param>
    /// <returns>200 con la serie (más reciente primero), 404 si el producto no existe, 400 si el rango está invertido.</returns>
    Task<Result<IReadOnlyList<HistorialPrecioDto>, DomainError>> GetHistorialPreciosAsync(
        long productoId, DateTime? desde, DateTime? hasta, int limit);

    /// <summary>
    /// Registra la vista de un usuario sobre un producto (escritura Q-B).
    /// Denormaliza el nombre del producto en la fila; el TTL (180 días) lo aplica la tabla.
    /// </summary>
    /// <param name="usuarioId">Usuario autenticado (partition key del feed).</param>
    /// <param name="productoId">Producto visto.</param>
    /// <returns>204 siempre que el producto exista; 404 si no existe.</returns>
    Task<UnitResult<DomainError>> RegistrarVistaAsync(long usuarioId, long productoId);

    /// <summary>
    /// Feed de actividad: últimos productos vistos por un usuario (consulta Q-B).
    /// No puede fallar por "no encontrado": el usuario está autenticado y una
    /// partición vacía es una respuesta válida (lista vacía), no un error.
    /// </summary>
    /// <param name="usuarioId">Usuario autenticado.</param>
    /// <param name="limit">Tamaño del feed (se acota a 1..100; por defecto 20).</param>
    /// <returns>Entradas del feed, de la más reciente a la más antigua (vacío si el usuario aún no tiene vistas).</returns>
    Task<IReadOnlyList<VistaFeedDto>> GetFeedVistasAsync(long usuarioId, int limit);
}
