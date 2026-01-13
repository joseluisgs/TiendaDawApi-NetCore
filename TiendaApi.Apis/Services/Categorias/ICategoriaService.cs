using CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Categorias;

/// <summary>
/// Interfaz del servicio de categorías usando Patrón Result.
/// </summary>
public interface ICategoriaService
{
    Task<Result<IEnumerable<CategoriaDto>, DomainError>> FindAllAsync();
    Task<Result<PagedResult<CategoriaDto>, DomainError>> FindAllPagedAsync(CategoriaFilterDto filter);
    Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id);
    Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto);
    Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto);
    Task<UnitResult<DomainError>> DeleteAsync(long id);
}
