using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// Contrato del servicio de productos.
/// </summary>
public interface IProductoService
{
    /// <summary>Obtiene todos los productos.</summary>
    /// <returns>Resultado con colección de productos.</returns>
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync();

    /// <summary>Obtiene productos paginados con filtros.</summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Resultado con productos paginados.</returns>
    Task<Result<PagedResult<ProductoDto>, DomainError>> FindAllPagedAsync(ProductoFilterDto filter);

    /// <summary>Busca un producto por ID.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>Resultado con el producto o error.</returns>
    Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id);

    /// <summary>Obtiene productos por categoría.</summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>Resultado con colección de productos.</returns>
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>Crea un nuevo producto.</summary>
    /// <param name="dto">Datos del producto.</param>
    /// <returns>Resultado con el producto creado.</returns>
    Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto);

    /// <summary>Actualiza un producto existente.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Resultado con el producto actualizado.</returns>
    Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto);

    /// <summary>Elimina un producto (soft delete).</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteAsync(long id);

    /// <summary>Actualiza la imagen de un producto.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="image">Archivo de imagen.</param>
    /// <returns>Resultado con el producto actualizado.</returns>
    Task<Result<ProductoDto, DomainError>> UpdateImageAsync(long id, IFormFile image);

    /// <summary>Actualiza parcialmente un producto.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="dto">Campos a actualizar.</param>
    /// <returns>Resultado con el producto actualizado.</returns>
    Task<Result<ProductoDto, DomainError>> UpdatePartialAsync(long id, ProductoPatchDto dto);
}
