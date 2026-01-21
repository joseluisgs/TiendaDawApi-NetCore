using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Repositories.Categorias;

namespace TiendaApi.Apis.GraphQL.Queries;

/// <summary>
/// Consultas GraphQL de la tienda.
/// </summary>
public class TiendaQuery
{
    /// <summary>Obtiene todos los productos (proyección habilitada).</summary>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>IQueryable de productos.</returns>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Producto> GetProductos([Service] IProductoRepository productoRepository) =>
        productoRepository.FindAllAsNoTracking();

    /// <summary>Obtiene un producto por ID.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Producto encontrado o null.</returns>
    [UseFirstOrDefault]
    public async Task<Producto?> GetProducto(long id, [Service] IProductoRepository productoRepository) =>
        await productoRepository.FindByIdAsync(id);

    /// <summary>Obtiene productos paginados.</summary>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>IQueryable de productos paginados.</returns>
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Producto> GetProductosPaged([Service] IProductoRepository productoRepository) =>
        productoRepository.FindAllAsNoTracking();

    /// <summary>Obtiene todas las categorías.</summary>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>IQueryable de categorías.</returns>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Categoria> GetCategorias([Service] ICategoriaRepository categoriaRepository) =>
        categoriaRepository.FindAllAsNoTracking();

    /// <summary>Obtiene una categoría por ID.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Categoría encontrada o null.</returns>
    [UseFirstOrDefault]
    public async Task<Categoria?> GetCategoria(long id, [Service] ICategoriaRepository categoriaRepository) =>
        await categoriaRepository.FindByIdAsync(id);

    /// <summary>Obtiene categorías paginadas.</summary>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>IQueryable de categorías paginadas.</returns>
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Categoria> GetCategoriasPaged([Service] ICategoriaRepository categoriaRepository) =>
        categoriaRepository.FindAllAsNoTracking();
}
