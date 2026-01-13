using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Productos;

/// <summary>
/// Interfaz del servicio de productos usando Patrón Result.
/// </summary>
public interface IProductoService
{
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync();
    Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id);
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId);
    Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto);
    Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto);
    Task<UnitResult<DomainError>> DeleteAsync(long id);
    Task<Result<ProductoDto, DomainError>> UpdateImageAsync(long id, IFormFile image);
    Task<Result<ProductoDto, DomainError>> UpdatePartialAsync(long id, ProductoPatchDto dto);
}
