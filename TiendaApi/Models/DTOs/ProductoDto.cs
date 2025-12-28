namespace TiendaApi.Models.DTOs;

/// <summary>
/// DTO for Producto responses
/// </summary>
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

/// <summary>
/// DTO for creating/updating Producto
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
