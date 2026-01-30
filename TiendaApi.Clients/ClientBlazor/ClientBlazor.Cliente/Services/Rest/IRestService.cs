using CSharpFunctionalExtensions;
using ClientBlazor.Cliente.DTOs.Common;
using ClientBlazor.Cliente.DTOs.Productos;
using ClientBlazor.Cliente.DTOs.Categorias;
using ClientBlazor.Cliente.Domain.Errors;

namespace ClientBlazor.Cliente.Services.Rest;

/// <summary>
/// Define el contrato para el servicio principal de comunicación REST.
/// Gestiona las operaciones CRUD sobre los recursos de la API.
/// </summary>
public interface IRestService
{
    /// <summary>
    /// Obtiene una lista paginada de productos filtrada por diversos criterios.
    /// </summary>
    /// <param name="filter">Objeto con los criterios de filtrado y paginación.</param>
    /// <returns>Resultado con el listado paginado o error de dominio.</returns>
    Task<Result<PagedResult<ProductoDto>, DomainError>> GetProductosAsync(ProductoFilterDto filter);

    /// <summary>
    /// Recupera los detalles de un producto específico mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador único del producto.</param>
    /// <returns>Resultado con el producto o error 404/red.</returns>
    Task<Result<ProductoDto, DomainError>> GetProductoByIdAsync(long id);

    /// <summary>
    /// Crea un nuevo producto en el catálogo.
    /// </summary>
    /// <param name="request">Datos del nuevo producto.</param>
    /// <returns>Resultado con el producto creado.</returns>
    Task<Result<ProductoDto, DomainError>> CreateProductoAsync(ProductoRequestDto request);

    /// <summary>
    /// Actualiza los datos de un producto existente.
    /// </summary>
    /// <param name="id">ID del producto a modificar.</param>
    /// <param name="request">Nuevos datos.</param>
    /// <returns>Resultado con el producto actualizado.</returns>
    Task<Result<ProductoDto, DomainError>> UpdateProductoAsync(long id, ProductoRequestDto request);

    /// <summary>
    /// Elimina un producto del catálogo (borrado lógico en API).
    /// </summary>
    /// <param name="id">ID del producto a eliminar.</param>
    /// <returns>Resultado exitoso si se eliminó correctamente.</returns>
    Task<Result<bool, DomainError>> DeleteProductoAsync(long id);

    /// <summary>
    /// Obtiene una lista paginada de categorías disponibles.
    /// </summary>
    /// <param name="filter">Filtros de búsqueda.</param>
    /// <returns>Resultado con las categorías.</returns>
    Task<Result<PagedResult<CategoriaDto>, DomainError>> GetCategoriasAsync(CategoriaFilterDto filter);

    /// <summary>
    /// Recupera una categoría por su identificador único.
    /// </summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>Resultado con la categoría encontrada.</returns>
    Task<Result<CategoriaDto, DomainError>> GetCategoriaByIdAsync(long id);

    /// <summary>
    /// Crea una nueva categoría de productos.
    /// </summary>
    /// <param name="request">Nombre de la categoría.</param>
    /// <returns>Resultado con la categoría creada.</returns>
    Task<Result<CategoriaDto, DomainError>> CreateCategoriaAsync(CategoriaRequestDto request);

    /// <summary>
    /// Modifica los datos de una categoría existente.
    /// </summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="request">Nuevos datos.</param>
    /// <returns>Resultado con la categoría actualizada.</returns>
    Task<Result<CategoriaDto, DomainError>> UpdateCategoriaAsync(long id, CategoriaRequestDto request);

    /// <summary>
    /// Elimina una categoría del sistema.
    /// </summary>
    /// <param name="id">ID de la categoría a borrar.</param>
    /// <returns>Resultado exitoso.</returns>
    Task<Result<bool, DomainError>> DeleteCategoriaAsync(long id);
}