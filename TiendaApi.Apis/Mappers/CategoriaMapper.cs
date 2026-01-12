using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Métodos de extensión para mapeo de categorías.
/// Alternativa a AutoMapper con fines educativos.
/// </summary>
public static class CategoriaMapper
{
    /// <summary>
    /// Convierte una categoría a DTO.
    /// Returns: CategoriaDto
    /// </summary>
    public static CategoriaDto ToDto(this Categoria categoria)
    {
        return new CategoriaDto
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            CreatedAt = categoria.CreatedAt
        };
    }

    /// <summary>
    /// Convierte una lista de categorías a lista de DTOs.
    /// Returns: IEnumerable<CategoriaDto>
    /// </summary>
    public static IEnumerable<CategoriaDto> ToDtoList(this IEnumerable<Categoria> categorias)
    {
        return categorias.Select(c => c.ToDto());
    }

    /// <summary>
    /// Convierte un DTO de solicitud a entidad categoría.
    /// Returns: Categoria
    /// </summary>
    public static Categoria ToEntity(this CategoriaRequestDto dto)
    {
        return new Categoria
        {
            Nombre = dto.Nombre,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Actualiza una entidad categoría con datos del DTO de solicitud.
    /// Returns: void
    /// </summary>
    public static void UpdateEntity(this CategoriaRequestDto dto, Categoria categoria)
    {
        categoria.Nombre = dto.Nombre;
        categoria.UpdatedAt = DateTime.UtcNow;
    }
}
