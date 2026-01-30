using ClientBlazor.Cliente.DTOs.Categorias;
using ClientBlazor.Cliente.Domain.Models;

namespace ClientBlazor.Cliente.Domain.Mappers;

/// <summary>
/// Proporciona métodos de extensión para transformar DTOs de categorías en modelos de UI.
/// </summary>
public static class CategoriaMapper
{
    /// <summary>
    /// Transforma un <see cref="CategoriaDto"/> en un <see cref="CategoriaModel"/>.
    /// </summary>
    /// <param name="dto">DTO de la API.</param>
    /// <returns>Modelo de UI.</returns>
    public static CategoriaModel ToModel(this CategoriaDto dto)
    {
        return new CategoriaModel
        {
            Id = dto.Id,
            Nombre = dto.Nombre,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// Transforma una colección de DTOs en modelos de UI.
    /// </summary>
    /// <param name="dtos">Colección de entrada.</param>
    /// <returns>Colección de salida.</returns>
    public static IEnumerable<CategoriaModel> ToModel(this IEnumerable<CategoriaDto> dtos)
    {
        return dtos.Select(d => d.ToModel());
    }
}
