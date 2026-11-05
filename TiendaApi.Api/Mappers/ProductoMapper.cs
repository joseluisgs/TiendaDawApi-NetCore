using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Mapper para convertir entre Producto y sus DTOs.
/// </summary>
public static class ProductoMapper
{
    /// <summary>Convierte Producto a ProductoDto.</summary>
    public static ProductoDto ToDto(this Producto producto) =>
        new(
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock,
            producto.Imagen,
            new CategoriaResumenDto(
                producto.CategoriaId,
                producto.Categoria?.Nombre ?? string.Empty,
                producto.Categoria?.Descripcion),
            producto.Proveedor is null
                ? null
                : new ProveedorResumenDto(
                    producto.Proveedor.Codigo,
                    producto.Proveedor.Nombre,
                    producto.Proveedor.Descripcion),
            producto.CreatedAt,
            producto.UpdatedAt
        );

    /// <summary>Convierte lista de Productos a lista de ProductoDto.</summary>
    public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<Producto> productos) =>
        productos.Select(p => p.ToDto());

    /// <summary>Convierte ProductoRequestDto a Producto.</summary>
    public static Producto ToEntity(this ProductoRequestDto dto) => new()
    {
        Nombre = dto.Nombre,
        Descripcion = dto.Descripcion,
        Precio = dto.Precio,
        Stock = dto.Stock,
        Imagen = dto.Imagen,
        CategoriaId = dto.CategoriaId,
        // Un código vacío no es una clave válida: se trata como "sin proveedor".
        ProveedorId = string.IsNullOrWhiteSpace(dto.ProveedorCodigo) ? null : dto.ProveedorCodigo,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>Actualiza Producto desde ProductoRequestDto (PUT: reemplaza todos los campos editables).</summary>
    public static void UpdateEntity(this ProductoRequestDto dto, Producto producto)
    {
        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.CategoriaId = dto.CategoriaId;
        producto.ProveedorId = string.IsNullOrWhiteSpace(dto.ProveedorCodigo) ? null : dto.ProveedorCodigo;
        if (!string.IsNullOrEmpty(dto.Imagen))
            producto.Imagen = dto.Imagen;
    }
}
