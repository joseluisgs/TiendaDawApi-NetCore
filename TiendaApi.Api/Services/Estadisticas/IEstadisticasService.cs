using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Estadisticas;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Estadisticas;

/// <summary>
///     Contrato del servicio de estadísticas.
///     🎓 Todas las vistas se calculan al vuelo sobre los datos existentes: no hay
///     tablas de agregados, trabajos de fondo ni materialización. Cada llamada carga
///     la ventana pedida y delega el cálculo en <see cref="EstadisticasCalculator" />.
/// </summary>
public interface IEstadisticasService {
    /// <summary>Resumen general con comparativa frente al periodo anterior.</summary>
    Task<Result<ResumenEstadisticasDto, DomainError>> ResumenAsync(DateTime? desde, DateTime? hasta);

    /// <summary>Serie temporal de ventas agrupada por día, semana o mes.</summary>
    Task<Result<VentasSerieDto, DomainError>> VentasAsync(DateTime? desde, DateTime? hasta, string agrupar);

    /// <summary>Ranking de productos vendidos por unidades o por recaudación.</summary>
    Task<Result<ProductosMasVendidosDto, DomainError>> ProductosMasVendidosAsync(
        DateTime? desde, DateTime? hasta, string orden, int top);

    /// <summary>Rendimiento por categoría (incluye las categorías sin ventas).</summary>
    Task<Result<CategoriasEstadisticasDto, DomainError>> CategoriasAsync(DateTime? desde, DateTime? hasta);

    /// <summary>Rendimiento por proveedor.</summary>
    Task<Result<ProveedoresEstadisticasDto, DomainError>> ProveedoresAsync(DateTime? desde, DateTime? hasta);

    /// <summary>Embudo de estados con los cancelados aparte.</summary>
    Task<Result<EstadosEstadisticasDto, DomainError>> EstadosAsync(DateTime? desde, DateTime? hasta);

    /// <summary>Clientes con más gasto en la ventana.</summary>
    Task<Result<ClientesEstadisticasDto, DomainError>> ClientesAsync(DateTime? desde, DateTime? hasta, int top);

    /// <summary>Estado del inventario (no depende de la ventana: es el catálogo actual).</summary>
    Task<Result<InventarioDto, DomainError>> InventarioAsync(int stockMax);

    /// <summary>Evolución del precio de un producto desde el histórico de Cassandra.</summary>
    Task<Result<PreciosEstadisticasDto, DomainError>> PreciosAsync(
        long productoId, DateTime? desde, DateTime? hasta);
}
