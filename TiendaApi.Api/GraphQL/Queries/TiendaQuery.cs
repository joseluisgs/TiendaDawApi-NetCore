using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Dtos.Common;

namespace TiendaApi.Api.GraphQL.Queries;

/// <summary>
/// Consultas GraphQL de la tienda.
/// </summary>
public class TiendaQuery
{
    /// <summary>Obtiene todos los productos.</summary>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>IQueryable de productos.</returns>
    public IQueryable<Producto> GetProductos([Service] IProductoRepository productoRepository) =>
        productoRepository.FindAllAsNoTracking();

    /// <summary>Obtiene un producto por ID.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Producto encontrado o null.</returns>
    public async Task<Producto?> GetProducto(long id, [Service] IProductoRepository productoRepository) =>
        await productoRepository.FindByIdAsync(id);

    /// <summary>Obtiene productos paginados.</summary>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Resultado paginado de productos.</returns>
    public async Task<PagedResult<ProductoDto>> GetProductosPaged(
        [Service] IProductoRepository productoRepository,
        int page = 1,
        int size = 10)
    {
        var filter = new ProductoFilterDto(null, null, null, null, null, page, size);
        var result = await productoRepository.FindAllPagedAsync(filter);
        return new PagedResult<ProductoDto>
        {
            Items = result.Items.Select(p => new ProductoDto(
                p.Id, p.Nombre, p.Descripcion, p.Precio, p.Stock,
                p.Imagen, p.CategoriaId, p.Categoria?.Nombre ?? "", p.CreatedAt, p.UpdatedAt)),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = size
        };
    }

    /// <summary>Obtiene todas las categorías.</summary>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>IQueryable de categorías.</returns>
    public IQueryable<Categoria> GetCategorias([Service] ICategoriaRepository categoriaRepository) =>
        categoriaRepository.FindAllAsNoTracking();

    /// <summary>Obtiene una categoría por ID.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Categoría encontrada o null.</returns>
    public async Task<Categoria?> GetCategoria(long id, [Service] ICategoriaRepository categoriaRepository) =>
        await categoriaRepository.FindByIdAsync(id);

    /// <summary>Obtiene categorías paginadas.</summary>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Resultado paginado de categorías.</returns>
    public async Task<PagedResult<CategoriaDto>> GetCategoriasPaged(
        [Service] ICategoriaRepository categoriaRepository,
        int page = 1,
        int size = 10)
    {
        var filter = new CategoriaFilterDto { Nombre = null, Page = page, Size = size };
        var result = await categoriaRepository.FindAllPagedAsync(filter);
        return new PagedResult<CategoriaDto>
        {
            Items = result.Items.Select(c => new CategoriaDto(c.Id, c.Nombre, c.Descripcion, c.CreatedAt, c.UpdatedAt)),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = size
        };
    }
}
