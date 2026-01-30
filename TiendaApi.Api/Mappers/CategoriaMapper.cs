using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Proporciona métodos de extensión para la transformación entre entidades de base de datos y DTOs de categorías.
/// </summary>
public static class CategoriaMapper
{
    /// <summary>
    /// Transforma una entidad <see cref="Categoria"/> en su representación <see cref="CategoriaDto"/>.
    /// </summary>
    /// <param name="categoria">La entidad de origen.</param>
    /// <returns>El DTO de visualización.</returns>
    public static CategoriaDto ToDto(this Categoria categoria) =>
        new()
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            CreatedAt = categoria.CreatedAt,
            UpdatedAt = categoria.UpdatedAt
        };

    /// <summary>
    /// Transforma una colección de entidades en una colección de DTOs.
    /// </summary>
    /// <param name="categorias">Colección de origen.</param>
    /// <returns>Colección de DTOs mapeados.</returns>
    public static IEnumerable<CategoriaDto> ToDtoList(this IEnumerable<Categoria> categorias) =>
        categorias.Select(c => c.ToDto());

    /// <summary>
    /// Crea una nueva entidad <see cref="Categoria"/> a partir de un DTO de creación.
    /// </summary>
    /// <param name="dto">Datos de entrada.</param>
    /// <returns>Una nueva instancia de la entidad.</returns>
    public static Categoria ToEntity(this CategoriaRequestDto dto) => new()
    {
        Nombre = dto.Nombre,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}