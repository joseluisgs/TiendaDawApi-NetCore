using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Categorias;

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
}
