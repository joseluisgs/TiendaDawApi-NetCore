using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Errors;

namespace TiendaApi.Services.Categorias;

/// <summary>
/// Interfaz del servicio de categorías usando Patrón Result.
/// </summary>
public interface ICategoriaService
{
    Task<Result<IEnumerable<CategoriaDto>, DomainError>> FindAllAsync();
    Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id);
    Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto);
    Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto);
    Task<UnitResult<DomainError>> DeleteAsync(long id);
}
