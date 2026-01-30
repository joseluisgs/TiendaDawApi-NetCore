namespace ClientBlazor.Cliente.DTOs.Productos;

public record ProductoDto
{
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string? Imagen { get; init; }
    public long CategoriaId { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ProductoRequestDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string? Imagen { get; init; }
    public long CategoriaId { get; init; }
}

public record ProductoFilterDto
{
    public string? Nombre { get; init; }
    public string? Categoria { get; init; }
    public bool? IsDeleted { get; init; }
    public decimal? PrecioMax { get; init; }
    public int? StockMin { get; init; }
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string SortBy { get; init; } = "id";
    public string Direction { get; init; } = "asc";
}
