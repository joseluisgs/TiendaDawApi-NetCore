using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.GraphQL.Queries;

/// <summary>
/// Consultas GraphQL de la tienda.
///
/// 🎓 Seguridad: GraphQL solo expone DTOs, nunca entidades del modelo de escritura.
/// Esto evita que el cliente componga consultas arbitrarias sobre la BD interna.
/// </summary>
public class TiendaQuery
{
    /// <summary>Obtiene todos los productos como DTOs.</summary>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Lista de productos (DTOs, no entidades).</returns>
    public async Task<IReadOnlyList<ProductoDto>> GetProductos(
        [Service] IProductoRepository productoRepository)
    {
        var productos = await productoRepository.FindAllAsync();
        return productos.ToDtoList().ToList();
    }

    /// <summary>Obtiene un producto por ID como DTO.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Producto encontrado (DTO) o null.</returns>
    public async Task<ProductoDto?> GetProducto(
        long id,
        [Service] IProductoRepository productoRepository)
    {
        var producto = await productoRepository.FindByIdAsync(id);
        return producto?.ToDto();
    }

    /// <summary>Obtiene productos paginados.</summary>
    /// <param name="page">Número de página (base 1, contrato GraphQL).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Resultado paginado de productos.</returns>
    public async Task<PagedResult<ProductoDto>> GetProductosPaged(
        [Service] IProductoRepository productoRepository,
        int page = 1,
        int size = 10)
    {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0 (Skip(Page*Size))
        var filter = new ProductoFilterDto(
            Nombre: null,
            Categoria: null,
            IsDeleted: null,
            PrecioMax: null,
            StockMin: null,
            Page: Math.Max(page - 1, 0),
            Size: size);

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

    /// <summary>Obtiene todas las categorías como DTOs.</summary>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Lista de categorías (DTOs, no entidades).</returns>
    public async Task<IReadOnlyList<CategoriaDto>> GetCategorias(
        [Service] ICategoriaRepository categoriaRepository)
    {
        var categorias = await categoriaRepository.FindAllAsync();
        return categorias.Select(c => c.ToDto()).ToList();
    }

    /// <summary>Obtiene una categoría por ID como DTO.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Categoría encontrada (DTO) o null.</returns>
    public async Task<CategoriaDto?> GetCategoria(
        long id,
        [Service] ICategoriaRepository categoriaRepository)
    {
        var categoria = await categoriaRepository.FindByIdAsync(id);
        return categoria?.ToDto();
    }

    /// <summary>Obtiene categorías paginadas.</summary>
    /// <param name="page">Número de página (base 1, contrato GraphQL).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Resultado paginado de categorías.</returns>
    public async Task<PagedResult<CategoriaDto>> GetCategoriasPaged(
        [Service] ICategoriaRepository categoriaRepository,
        int page = 1,
        int size = 10)
    {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0 (Skip(Page*Size))
        var filter = new CategoriaFilterDto
        {
            Nombre = null,
            Page = Math.Max(page - 1, 0),
            Size = size
        };
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
