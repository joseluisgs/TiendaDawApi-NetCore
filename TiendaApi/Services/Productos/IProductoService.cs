using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Productos;
using TiendaApi.Errors;

namespace TiendaApi.Services.Productos;

/// <summary>
/// Interface for Producto service using Result Pattern
/// Railway Oriented Programming approach for error handling
/// </summary>
public interface IProductoService
{
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync();
    Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id);
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId);
    Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto);
    Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto);
    Task<UnitResult<DomainError>> DeleteAsync(long id);
}
