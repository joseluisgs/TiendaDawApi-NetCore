using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Categorias;

/// <summary>
/// Contrato del servicio de categorías.
/// </summary>
public interface ICategoriaService
{
    /// <summary>Obtiene todas las categorías.</summary>
    /// <returns>Resultado con colección de categorías.</returns>
    Task<Result<IEnumerable<CategoriaDto>, DomainError>> FindAllAsync();

    /// <summary>Obtiene categorías paginadas con filtros.</summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Resultado con categorías paginadas.</returns>
    Task<Result<PagedResult<CategoriaDto>, DomainError>> FindAllPagedAsync(CategoriaFilterDto filter);

    /// <summary>Busca una categoría por ID.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>Resultado con la categoría o error.</returns>
    Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id);

    /// <summary>Crea una nueva categoría.</summary>
    /// <param name="dto">Datos de la categoría a crear.</param>
    /// <returns>Resultado con la categoría creada.</returns>
    Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto);

    /// <summary>Actualiza una categoría existente.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Resultado con la categoría actualizada.</returns>
    Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto);

    /// <summary>Elimina una categoría (soft delete).</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteAsync(long id);
}
