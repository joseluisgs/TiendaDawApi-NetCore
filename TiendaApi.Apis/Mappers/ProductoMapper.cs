using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Métodos de extensión para mapeo de productos.
/// Alternativa a AutoMapper con fines educativos.
/// </summary>
public static class ProductoMapper
{
    /// <summary>
    /// Convierte un producto a DTO.
    /// Devuelve: ProductoDto
    /// </summary>
    public static ProductoDto ToDto(this Producto producto)
    {
        return new ProductoDto(
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock,
            producto.Imagen,
            producto.CategoriaId,
            producto.Categoria?.Nombre ?? string.Empty,
            producto.CreatedAt,
            producto.UpdatedAt
        );
    }

    /// <summary>
    /// Convierte una lista de productos a lista de DTOs.
    /// Devuelve: IEnumerable<ProductoDto>
    /// </summary>
    public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<Producto> productos)
    {
        return productos.Select(p => p.ToDto());
    }

    /// <summary>
    /// Convierte un DTO de solicitud a entidad producto.
    /// Devuelve: Producto
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
    /// Actualiza una entidad producto con datos del DTO de solicitud.
    /// Devuelve: void (no retorna valor)
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
    }
}
