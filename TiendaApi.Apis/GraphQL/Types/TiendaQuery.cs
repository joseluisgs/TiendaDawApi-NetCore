using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Repositories.Categorias;

namespace TiendaApi.Apis.GraphQL.Types;

/// <summary>
/// Tipo de consulta raíz de GraphQL para la tienda.
/// Expone las consultas de productos y categorías.
/// </summary>
public class TiendaQuery
{
    /// <summary>
    /// Consulta para obtener todos los productos.
    /// </summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Producto> GetProductos([Service] IProductoRepository productoRepository)
    {
        return productoRepository.FindAllAsNoTracking();
    }

    /// <summary>
    /// Consulta para obtener un producto por su ID.
    /// </summary>
    [UseFirstOrDefault]
    public async Task<Producto?> GetProducto(
        long id,
        [Service] IProductoRepository productoRepository)
    {
        return await productoRepository.FindByIdAsync(id);
    }

    /// <summary>
    /// Consulta para obtener todos los productos paginados.
    /// </summary>
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Producto> GetProductosPaged(
        [Service] IProductoRepository productoRepository)
    {
        return productoRepository.FindAllAsNoTracking();
    }

    /// <summary>
    /// Consulta para obtener todas las categorías.
    /// </summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Categoria> GetCategorias([Service] ICategoriaRepository categoriaRepository)
    {
        return categoriaRepository.FindAllAsNoTracking();
    }

    /// <summary>
    /// Consulta para obtener una categoría por su ID.
    /// </summary>
    [UseFirstOrDefault]
    public async Task<Categoria?> GetCategoria(
        long id,
        [Service] ICategoriaRepository categoriaRepository)
    {
        return await categoriaRepository.FindByIdAsync(id);
    }

    /// <summary>
    /// Consulta para obtener todas las categorías paginadas.
    /// </summary>
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Categoria> GetCategoriasPaged(
        [Service] ICategoriaRepository categoriaRepository)
    {
        return categoriaRepository.FindAllAsNoTracking();
    }
}
