using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Proporciona métodos de extensión para la transformación entre entidades de base de datos y DTOs de productos.
/// </summary>
public static class ProductoMapper
{
    /// <summary>
    /// Transforma una entidad <see cref="Producto"/> en su representación <see cref="ProductoDto"/>.
    /// </summary>
    /// <param name="producto">La entidad de origen.</param>
    /// <returns>El DTO de visualización.</returns>
    public static ProductoDto ToDto(this Producto producto) =>
        new()
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

    /// <summary>
    /// Transforma una colección de entidades en una colección de DTOs.
    /// </summary>
    /// <param name="productos">Colección de origen.</param>
    /// <returns>Colección de DTOs mapeados.</returns>
    public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<Producto> productos) =>
        productos.Select(p => p.ToDto());

    /// <summary>
    /// Crea una nueva entidad <see cref="Producto"/> a partir de un DTO de creación.
    /// </summary>
    /// <param name="dto">Datos de entrada.</param>
    /// <returns>Una nueva instancia de la entidad.</returns>
    public static Producto ToEntity(this ProductoRequestDto dto) => new()
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

    /// <summary>
    /// Actualiza el estado de una entidad existente con los datos provenientes de un DTO.
    /// </summary>
    /// <param name="dto">Nuevos datos.</param>
    /// <param name="producto">Entidad a modificar.</param>
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