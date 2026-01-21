using CSharpFunctionalExtensions;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Background.Jobs;

/// <summary>
/// Define el contrato para el servicio de reportes de productos.
/// </summary>
public interface IProductoReportTask
{
    /// <summary>
    /// Ejecuta el reporte de productos nuevos y envía notificaciones por email.
    /// En desarrollo, solo registra un log informativo.
    /// En producción, obtiene productos de los últimos X días y envía emails a usuarios activos.
    /// </summary>
    /// <returns>UnitResult.Success o UnitResult.Failure(DomainError).</returns>
    Task<UnitResult<DomainError>> ExecuteAsync();
}
