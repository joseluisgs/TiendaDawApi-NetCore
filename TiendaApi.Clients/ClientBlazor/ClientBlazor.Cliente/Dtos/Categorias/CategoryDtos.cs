namespace ClientBlazor.Cliente.DTOs.Categorias;

/// <summary>
/// DTO de categoría para respuestas de API.
/// Copia exacta del CategoriaDto de la API.
/// </summary>
public record CategoriaDto(
    long Id,
    string Nombre,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO de categoría para solicitudes de creación.
/// Copia exacta del CategoriaRequestDto de la API.
/// </summary>
public record CategoriaRequestDto
{
    public string Nombre { get; init; } = string.Empty;
}

/// <summary>
/// DTO de filtros para búsqueda de categorías.
/// Copia exacta del CategoriaFilterDto de la API.
/// </summary>
public record CategoriaFilterDto(
    string? Nombre,
    int Page = 0,
    int Size = 10,
    string SortBy = "id",
    string Direction = "asc"
);