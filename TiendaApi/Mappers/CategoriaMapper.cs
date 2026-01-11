using TiendaApi.Dtos.Categorias;
using TiendaApi.Models;

namespace TiendaApi.Mappers;

/// <summary>
/// Extension methods for Categoria entity-DTO conversions
/// Alternative to AutoMapper for educational purposes
/// </summary>
public static class CategoriaMapper
{
    /// <summary>
    /// Converts Categoria entity to CategoriaDto
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
    /// Converts IEnumerable<Categoria> to IEnumerable<CategoriaDto>
    /// </summary>
    public static IEnumerable<CategoriaDto> ToDtoList(this IEnumerable<Categoria> categorias)
    {
        return categorias.Select(c => c.ToDto());
    }

    /// <summary>
    /// Converts CategoriaRequestDto to Categoria entity
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
    /// Updates an existing Categoria entity with data from CategoriaRequestDto
    /// </summary>
    public static void UpdateEntity(this CategoriaRequestDto dto, Categoria categoria)
    {
        categoria.Nombre = dto.Nombre;
        categoria.UpdatedAt = DateTime.UtcNow;
    }
}
