namespace ClientBlazor.Cliente.DTOs.Productos;

public record ProductoDto {
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string? Imagen { get; init; }
    public CategoriaResumenDto Categoria { get; init; } = new();
    public ProveedorResumenDto? Proveedor { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Categoría de un producto, anidada en la respuesta de la API.</summary>
public record CategoriaResumenDto {
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Descripción de la categoría (null si la categoría no tiene).</summary>
    public string? Descripcion { get; init; }
}

/// <summary>Proveedor de un producto, anidado en la respuesta de la API. Null si no tiene.</summary>
public record ProveedorResumenDto {
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Descripción del proveedor (null si no tiene).</summary>
    public string? Descripcion { get; init; }
}

public record ProductoRequestDto {
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string? Imagen { get; init; }
    public long CategoriaId { get; init; }
}

public record ProductoFilterDto {
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
