namespace ClientBlazor.Cliente.DTOs.Productos;

/// <summary>
/// DTO de producto para respuestas de API.
/// Copia exacta del ProductoDto de la API.
/// </summary>
public record ProductoDto(
    long Id,
    string Nombre,
    string Descripcion,
    decimal Precio,
    int Stock,
    string? Imagen,
    long CategoriaId,
    string CategoriaNombre,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO de producto para solicitudes de creación.
/// Copia exacta del ProductoRequestDto de la API.
/// </summary>
public record ProductoRequestDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string? Imagen { get; init; }
    public long CategoriaId { get; init; }
}

/// <summary>
/// DTO de filtros para búsqueda de productos.
/// Copia exacta del ProductoFilterDto de la API.
/// </summary>
public record ProductoFilterDto(
    string? Nombre,
    string? Categoria,
    bool? IsDeleted,
    decimal? PrecioMax,
    int? StockMin,
    int Page = 0,
    int Size = 10,
    string SortBy = "id",
    string Direction = "asc"
);