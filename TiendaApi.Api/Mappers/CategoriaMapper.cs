using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Mapper para convertir entre Categoria y sus DTOs.
/// </summary>
public static class CategoriaMapper
{
    /// <summary>Convierte Categoria a CategoriaDto.</summary>
    public static CategoriaDto ToDto(this Categoria categoria) =>
        new(categoria.Id, categoria.Nombre, categoria.Descripcion, categoria.CreatedAt, categoria.UpdatedAt);

    /// <summary>Convierte lista de Categorias a lista de CategoriaDto.</summary>
    public static IEnumerable<CategoriaDto> ToDtoList(this IEnumerable<Categoria> categorias) =>
        categorias.Select(c => c.ToDto());

    /// <summary>Convierte CategoriaRequestDto a Categoria.</summary>
    public static Categoria ToEntity(this CategoriaRequestDto dto) => new()
    {
        Nombre = dto.Nombre,
        Descripcion = dto.Descripcion,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>Actualiza Categoria desde CategoriaRequestDto.</summary>
    public static void UpdateEntity(this CategoriaRequestDto dto, Categoria categoria)
    {
        categoria.Nombre = dto.Nombre;
        categoria.Descripcion = dto.Descripcion;
    }
}
