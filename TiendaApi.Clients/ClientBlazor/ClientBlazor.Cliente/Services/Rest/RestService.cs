using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.DTOs.Common;
using ClientBlazor.Cliente.DTOs.Productos;
using ClientBlazor.Cliente.DTOs.Categorias;
using ClientBlazor.Cliente.Clients;
using CSharpFunctionalExtensions;
using Refit;
using System.Net;

namespace ClientBlazor.Cliente.Services.Rest;

/// <inheritdoc cref="IRestService" />
public class RestService(ITiendaRestClient client) : IRestService
{
    /// <inheritdoc cref="IRestService.GetProductosAsync(ProductoFilterDto)" />
    public async Task<Result<PagedResult<ProductoDto>, DomainError>> GetProductosAsync(ProductoFilterDto filter)
    {
        try
        {
            var result = await client.GetProductosAsync(filter);
            return Result.Success<PagedResult<ProductoDto>, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<PagedResult<ProductoDto>, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<PagedResult<ProductoDto>, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.GetProductoByIdAsync(long)" />
    public async Task<Result<ProductoDto, DomainError>> GetProductoByIdAsync(long id)
    {
        try
        {
            var result = await client.GetProductoByIdAsync(id);
            return Result.Success<ProductoDto, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<ProductoDto, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<ProductoDto, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.CreateProductoAsync(ProductoRequestDto)" />
    public async Task<Result<ProductoDto, DomainError>> CreateProductoAsync(ProductoRequestDto request)
    {
        try
        {
            var result = await client.CreateProductoAsync(request);
            return Result.Success<ProductoDto, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<ProductoDto, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<ProductoDto, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.UpdateProductoAsync(long, ProductoRequestDto)" />
    public async Task<Result<ProductoDto, DomainError>> UpdateProductoAsync(long id, ProductoRequestDto request)
    {
        try
        {
            var result = await client.UpdateProductoAsync(id, request);
            return Result.Success<ProductoDto, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<ProductoDto, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<ProductoDto, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.DeleteProductoAsync(long)" />
    public async Task<Result<bool, DomainError>> DeleteProductoAsync(long id)
    {
        try
        {
            await client.DeleteProductoAsync(id);
            return Result.Success<bool, DomainError>(true);
        }
        catch (ApiException ex) { return Result.Failure<bool, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<bool, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.GetCategoriasAsync(CategoriaFilterDto)" />
    public async Task<Result<PagedResult<CategoriaDto>, DomainError>> GetCategoriasAsync(CategoriaFilterDto filter)
    {
        try
        {
            var result = await client.GetCategoriasAsync(filter);
            return Result.Success<PagedResult<CategoriaDto>, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<PagedResult<CategoriaDto>, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<PagedResult<CategoriaDto>, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.GetCategoriaByIdAsync(long)" />
    public async Task<Result<CategoriaDto, DomainError>> GetCategoriaByIdAsync(long id)
    {
        try
        {
            var result = await client.GetCategoriaByIdAsync(id);
            return Result.Success<CategoriaDto, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<CategoriaDto, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<CategoriaDto, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.CreateCategoriaAsync(CategoriaRequestDto)" />
    public async Task<Result<CategoriaDto, DomainError>> CreateCategoriaAsync(CategoriaRequestDto request)
    {
        try
        {
            var result = await client.CreateCategoriaAsync(request);
            return Result.Success<CategoriaDto, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<CategoriaDto, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<CategoriaDto, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.UpdateCategoriaAsync(long, CategoriaRequestDto)" />
    public async Task<Result<CategoriaDto, DomainError>> UpdateCategoriaAsync(long id, CategoriaRequestDto request)
    {
        try
        {
            var result = await client.UpdateCategoriaAsync(id, request);
            return Result.Success<CategoriaDto, DomainError>(result);
        }
        catch (ApiException ex) { return Result.Failure<CategoriaDto, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<CategoriaDto, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <inheritdoc cref="IRestService.DeleteCategoriaAsync(long)" />
    public async Task<Result<bool, DomainError>> DeleteCategoriaAsync(long id)
    {
        try
        {
            await client.DeleteCategoriaAsync(id);
            return Result.Success<bool, DomainError>(true);
        }
        catch (ApiException ex) { return Result.Failure<bool, DomainError>(MapException(ex)); }
        catch (Exception) { return Result.Failure<bool, DomainError>(NetworkErrors.ConnectionFailed); }
    }

    /// <summary>
    /// Mapea las excepciones de red de Refit a errores del dominio cliente.
    /// </summary>
    /// <param name="ex">La excepción capturada.</param>
    /// <returns>Una instancia de <see cref="DomainError"/>.</returns>
    private static DomainError MapException(ApiException ex)
    {
        return ex.StatusCode switch
        {
            HttpStatusCode.NotFound => NetworkErrors.NotFound,
            HttpStatusCode.Unauthorized => AuthErrors.LoginRequired,
            HttpStatusCode.Forbidden => AuthErrors.InsufficientPermissions,
            _ => NetworkErrors.ServerError
        };
    }
}
