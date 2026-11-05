using ClientBlazor.Cliente.DTOs.Auth;
using ClientBlazor.Cliente.DTOs.Categorias;
using ClientBlazor.Cliente.DTOs.Common;
using ClientBlazor.Cliente.DTOs.Productos;
using Refit;

namespace ClientBlazor.Cliente.Clients;

/// <summary>
/// Cliente REST tipado generado por Refit.
/// Define las operaciones contra la API backend.
/// </summary>
public interface ITiendaRestClient
{
    // ========================================================================
    // AUTH
    // ========================================================================

    [Post("/api/v1/auth/signin")]
    Task<AuthResponseDto> LoginAsync([Body] LoginDto loginDto);

    // ========================================================================
    // PRODUCTOS
    // ========================================================================

    [Get("/api/v1/productos")]
    Task<PagedResult<ProductoDto>> GetProductosAsync([Query] ProductoFilterDto filter);

    [Get("/api/v1/productos/{id}")]
    Task<ProductoDto> GetProductoByIdAsync(long id);

    [Post("/api/v1/productos")]
    [Headers("Authorization: Bearer")]
    Task<ProductoDto> CreateProductoAsync([Body] ProductoRequestDto producto);

    [Put("/api/v1/productos/{id}")]
    [Headers("Authorization: Bearer")]
    Task<ProductoDto> UpdateProductoAsync(long id, [Body] ProductoRequestDto producto);

    [Delete("/api/v1/productos/{id}")]
    [Headers("Authorization: Bearer")]
    Task DeleteProductoAsync(long id);

    // ========================================================================
    // CATEGORÍAS
    // ========================================================================

    [Get("/api/v1/categorias")]
    Task<PagedResult<CategoriaDto>> GetCategoriasAsync([Query] CategoriaFilterDto filter);

    [Get("/api/v1/categorias/{id}")]
    Task<CategoriaDto> GetCategoriaByIdAsync(long id);

    [Post("/api/v1/categorias")]
    [Headers("Authorization: Bearer")]
    Task<CategoriaDto> CreateCategoriaAsync([Body] CategoriaRequestDto categoria);

    [Put("/api/v1/categorias/{id}")]
    [Headers("Authorization: Bearer")]
    Task<CategoriaDto> UpdateCategoriaAsync(long id, [Body] CategoriaRequestDto categoria);

    [Delete("/api/v1/categorias/{id}")]
    [Headers("Authorization: Bearer")]
    Task DeleteCategoriaAsync(long id);
}