using TiendaApi.Dtos.Productos;
using TiendaApi.Models;

namespace TiendaApi.Mappers;

/// <summary>
/// Extension methods for Producto entity-DTO conversions
/// Alternative to AutoMapper for educational purposes
/// </summary>
public static class ProductoMapper
{
    /// <summary>
    /// Converts Producto entity to ProductoDto
    /// </summary>
    public static ProductoDto ToDto(this Producto producto)
    {
        return new ProductoDto
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            Stock = producto.Stock,
            Imagen = producto.Imagen,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.Categoria?.Nombre ?? string.Empty,
            CreatedAt = producto.CreatedAt,
            UpdatedAt = producto.UpdatedAt
        };
    }

    /// <summary>
    /// Converts IEnumerable<Producto> to IEnumerable<ProductoDto>
    /// </summary>
    public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<Producto> productos)
    {
        return productos.Select(p => p.ToDto());
    }

    /// <summary>
    /// Converts ProductoRequestDto to Producto entity
    /// </summary>
    public static Producto ToEntity(this ProductoRequestDto dto)
    {
        return new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Stock = dto.Stock,
            Imagen = dto.Imagen,
            CategoriaId = dto.CategoriaId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates an existing Producto entity with data from ProductoRequestDto
    /// </summary>
    public static void UpdateEntity(this ProductoRequestDto dto, Producto producto)
    {
        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.CategoriaId = dto.CategoriaId;
        if (!string.IsNullOrEmpty(dto.Imagen))
            producto.Imagen = dto.Imagen;
        producto.UpdatedAt = DateTime.UtcNow;
    }
}
