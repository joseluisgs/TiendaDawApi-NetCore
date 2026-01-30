namespace TiendaApi.Api.Dtos.Productos;

/// <summary>
/// Parámetros de filtrado y ordenación para la búsqueda de productos.
/// </summary>
/// <param name="Nombre">Contiene el nombre.</param>
/// <param name="Categoria">Nombre de la categoría.</param>
/// <param name="IsDeleted">Estado de borrado lógico.</param>
/// <param name="PrecioMax">Precio máximo permitido.</param>
/// <param name="StockMin">Stock mínimo requerido.</param>
/// <param name="Page">Número de página (0-indexed).</param>
/// <param name="Size">Elementos por página.</param>
/// <param name="SortBy">Campo de ordenación.</param>
/// <param name="Direction">Dirección (asc/desc).</param>
public record ProductoFilterDto(
    string? Nombre = null,
    string? Categoria = null,
    bool? IsDeleted = null,
    decimal? PrecioMax = null,
    int? StockMin = null,
    int Page = 0,
    int Size = 10,
    string SortBy = "id",
    string Direction = "asc"
);