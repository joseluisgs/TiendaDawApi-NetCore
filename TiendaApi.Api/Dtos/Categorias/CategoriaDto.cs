namespace TiendaApi.Api.Dtos.Categorias;

/// <summary>
/// Objeto de transferencia para la visualización de una categoría.
/// </summary>
public record CategoriaDto
{
    /// <summary>Identificador único de la categoría.</summary>
    public long Id { get; init; }

    /// <summary>Nombre descriptivo de la categoría.</summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Fecha de creación del registro.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Fecha de última actualización.</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Objeto de transferencia para la creación o actualización de una categoría.
/// </summary>
public record CategoriaRequestDto
{
    /// <summary>Nombre de la categoría.</summary>
    public string Nombre { get; init; } = string.Empty;
}