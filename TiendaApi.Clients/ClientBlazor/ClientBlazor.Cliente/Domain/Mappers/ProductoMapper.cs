using ClientBlazor.Cliente.DTOs.Productos;
using ClientBlazor.Cliente.Domain.Models;

namespace ClientBlazor.Cliente.Domain.Mappers;

/// <summary>
/// Proporciona métodos de extensión para transformar DTOs de productos en modelos de UI.
/// </summary>
public static class ProductoMapper
{
    /// <summary>
    /// Transforma un <see cref="ProductoDto"/> en un <see cref="ProductoModel"/>.
    /// </summary>
    /// <param name="dto">DTO proveniente de la API.</param>
    /// <returns>Modelo enriquecido para la UI.</returns>
    public static ProductoModel ToModel(this ProductoDto dto)
    {
        return new ProductoModel
        {
            Id = dto.Id,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Stock = dto.Stock,
            Imagen = dto.Imagen,
            CategoriaId = dto.CategoriaId,
            CategoriaNombre = dto.CategoriaNombre,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// Transforma una colección de DTOs en una colección de modelos de UI.
    /// </summary>
    /// <param name="dtos">Lista de DTOs.</param>
    /// <returns>Listado de modelos.</returns>
    public static IEnumerable<ProductoModel> ToModel(this IEnumerable<ProductoDto> dtos)
    {
        return dtos.Select(d => d.ToModel());
    }
}
