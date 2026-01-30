using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// Define el contrato para la gestión de productos en el backend.
/// Maneja la lógica de negocio, validaciones, persistencia y notificaciones en tiempo real.
/// </summary>
public interface IProductoService
{
    /// <summary>
    /// Recupera la lista completa de productos activos en el sistema.
    /// </summary>
    /// <returns>Resultado con la colección de <see cref="ProductoDto"/>.</returns>
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Recupera productos aplicando criterios de filtrado y paginación.
    /// </summary>
    /// <param name="filter">Deltas de filtrado y opciones de página.</param>
    /// <returns>Resultado con el objeto paginado.</returns>
    Task<Result<PagedResult<ProductoDto>, DomainError>> FindAllPagedAsync(ProductoFilterDto filter);

    /// <summary>
    /// Busca un producto por su identificador numérico único.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>Resultado con el producto o error 404.</returns>
    Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id);

    /// <summary>
    /// Obtiene todos los productos pertenecientes a una categoría específica.
    /// </summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>Resultado con la lista de productos.</returns>
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>
    /// Registra un nuevo producto validando sus campos y la existencia de la categoría.
    /// </summary>
    /// <param name="dto">Datos del producto a crear.</param>
    /// <returns>Resultado con el producto guardado.</returns>
    Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto);

    /// <summary>
    /// Actualiza completamente un producto existente.
    /// </summary>
    /// <param name="id">ID del producto a modificar.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Resultado con el producto actualizado.</returns>
    Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto);

    /// <summary>
    /// Realiza un borrado lógico del producto en el sistema.
    /// </summary>
    /// <param name="id">ID del producto a eliminar.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteAsync(long id);

    /// <summary>
    /// Sube y asigna una nueva imagen a un producto.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="image">Archivo de imagen proveniente del cliente.</param>
    /// <returns>Resultado con el producto actualizado incluyendo la nueva ruta.</returns>
    Task<Result<ProductoDto, DomainError>> UpdateImageAsync(long id, IFormFile image);

    /// <summary>
    /// Actualiza únicamente los campos proporcionados de un producto.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="dto">Campos opcionales a modificar.</param>
    /// <returns>Resultado con el producto actualizado.</returns>
    Task<Result<ProductoDto, DomainError>> UpdatePartialAsync(long id, ProductoPatchDto dto);
}