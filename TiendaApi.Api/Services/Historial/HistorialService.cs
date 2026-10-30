using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Repositories.Historial;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.Services.Historial;

/// <inheritdoc />
/// <remarks>
/// 🎓 Consistencia eventual en un almacén de series: la existencia del producto
/// la certifica PostgreSQL (fuente de verdad → su 404 manda) y la serie la sirve
/// Cassandra. Si el nodo de Cassandra no responde, el servicio NO propaga la
/// excepción: lecturas devuelven vacío y la escritura se descarta con warning
/// (fail-open). La tienda entera sigue siendo usable sin histórico ni feed.
/// </remarks>
public class HistorialService(
    IProductoRepository productoRepository,
    IHistorialRepository historialRepository,
    ILogger<HistorialService> logger) : IHistorialService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<HistorialPrecioDto>, DomainError>> GetHistorialPreciosAsync(
        long productoId, DateTime? desde, DateTime? hasta, int limit)
    {
        var producto = await productoRepository.FindByIdAsync(productoId);
        if (producto is null)
            return Result.Failure<IReadOnlyList<HistorialPrecioDto>, DomainError>(
                ProductoError.NotFound(productoId));

        if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
            return Result.Failure<IReadOnlyList<HistorialPrecioDto>, DomainError>(
                new ValidationError(
                    "El rango de fechas no puede estar invertido",
                    new Dictionary<string, string[]> { ["desde"] = ["No puede ser posterior a 'hasta'"] }));

        var limitEfectivo = Math.Clamp(limit, 1, 200);

        try
        {
            var serie = await historialRepository.GetHistorialPreciosAsync(
                productoId, desde, hasta, limitEfectivo);
            return Result.Success<IReadOnlyList<HistorialPrecioDto>, DomainError>(serie);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Cassandra no disponible: el historial del producto {Id} responde vacio (modo degradado)", productoId);
            return Result.Success<IReadOnlyList<HistorialPrecioDto>, DomainError>(Array.Empty<HistorialPrecioDto>());
        }
    }

    /// <inheritdoc />
    public async Task<UnitResult<DomainError>> RegistrarVistaAsync(long usuarioId, long productoId)
    {
        var producto = await productoRepository.FindByIdAsync(productoId);
        if (producto is null)
            return UnitResult.Failure<DomainError>(ProductoError.NotFound(productoId));

        try
        {
            // El nombre se copia a la fila (denormalización): el feed no hará cruces.
            await historialRepository.RegistrarVistaAsync(usuarioId, producto.Id, producto.Nombre);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Cassandra: no se pudo registrar la vista del producto {Id} por el usuario {UsuarioId} (modo degradado)",
                productoId, usuarioId);
        }

        return UnitResult.Success<DomainError>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VistaFeedDto>> GetFeedVistasAsync(long usuarioId, int limit)
    {
        var limitEfectivo = Math.Clamp(limit, 1, 100);

        try
        {
            return await historialRepository.GetFeedVistasAsync(usuarioId, limitEfectivo);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Cassandra no disponible: el feed del usuario {UsuarioId} responde vacio (modo degradado)", usuarioId);
            return Array.Empty<VistaFeedDto>();
        }
    }
}
