using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Categorias;

/// <summary>
/// Define el contrato para la gestión de categorías de productos.
/// Proporciona operaciones de consulta, creación, actualización y eliminación lógica.
/// </summary>
public interface ICategoriaService
{
    /// <summary>
    /// Recupera todas las categorías activas en el sistema.
    /// </summary>
    /// <returns>Resultado con la colección de <see cref="CategoriaDto"/>.</returns>
    Task<Result<IEnumerable<CategoriaDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene categorías aplicando criterios de filtrado y paginación.
    /// </summary>
    /// <param name="filter">Filtros de búsqueda.</param>
    /// <returns>Resultado con el objeto paginado.</returns>
    Task<Result<PagedResult<CategoriaDto>, DomainError>> FindAllPagedAsync(CategoriaFilterDto filter);

    /// <summary>
    /// Busca una categoría por su identificador único.
    /// </summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>Resultado con la categoría o error 404.</returns>
    Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id);

    /// <summary>
    /// Registra una nueva categoría validando que el nombre no esté duplicado.
    /// </summary>
    /// <param name="dto">Datos de la categoría a crear.</param>
    /// <returns>Resultado con la categoría guardada.</returns>
    Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto);

    /// <summary>
    /// Actualiza los datos de una categoría existente.
    /// </summary>
    /// <param name="id">ID de la categoría a modificar.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Resultado con la categoría actualizada.</returns>
    Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto);

    /// <summary>
    /// Realiza el borrado lógico de una categoría.
    /// </summary>
    /// <param name="id">ID de la categoría a eliminar.</param>
    /// <returns>Resultado exitoso si se eliminó correctamente.</returns>
    Task<UnitResult<DomainError>> DeleteAsync(long id);
}