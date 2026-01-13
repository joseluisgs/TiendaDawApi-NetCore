namespace TiendaApi.Apis.Dtos.Common;

/// <summary>
/// DTO para filtrar y paginar usuarios.
/// </summary>
public record UserFilterDto
{
    /// <summary>
    /// Filtrar por nombre de usuario (búsqueda parcial).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Filtrar por correo electrónico (búsqueda parcial).
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Filtrar por estado de eliminación.
    /// </summary>
    public bool? IsDeleted { get; init; }

    /// <summary>
    /// Número de página (basado en 0).
    /// </summary>
    public int Page { get; init; } = 0;

    /// <summary>
    /// Tamaño de página.
    /// </summary>
    public int Size { get; init; } = 10;

    /// <summary>
    /// Campo por el que ordenar.
    /// </summary>
    public string SortBy { get; init; } = "id";

    /// <summary>
    /// Dirección de ordenación (asc o desc).
    /// </summary>
    public string Direction { get; init; } = "asc";
}
