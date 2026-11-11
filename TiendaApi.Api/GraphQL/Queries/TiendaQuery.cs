using System.Security.Claims;
using HotChocolate.Authorization;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.GraphQL.Queries;

/// <summary>
///     Consultas GraphQL de la tienda.
///     🎓 Seguridad: GraphQL solo expone DTOs, nunca entidades del modelo de escritura.
///     Esto evita que el cliente componga consultas arbitrarias sobre la BD interna.
/// </summary>
public class TiendaQuery {
    /// <summary>Obtiene todos los productos como DTOs.</summary>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Lista de productos (DTOs, no entidades).</returns>
    public async Task<IReadOnlyList<ProductoDto>> GetProductos(
        [Service] IProductoRepository productoRepository) {
        IEnumerable<Producto> productos = await productoRepository.FindAllAsync();
        return productos.ToDtoList().ToList();
    }

    /// <summary>Obtiene un producto por ID como DTO.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <returns>Producto encontrado (DTO) o null.</returns>
    public async Task<ProductoDto?> GetProducto(
        long id,
        [Service] IProductoRepository productoRepository) {
        Producto? producto = await productoRepository.FindByIdAsync(id);
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
        int size = 10) {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0 (Skip(Page*Size))
        var filter = new ProductoFilterDto(
            null,
            null,
            null,
            null,
            null,
            Math.Max(page - 1, 0),
            size);

        (IEnumerable<Producto> Items, int TotalCount) result = await productoRepository.FindAllPagedAsync(filter);
        return new PagedResult<ProductoDto> {
            Items = result.Items.Select(p => p.ToDto()),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = size
        };
    }

    /// <summary>Obtiene todas las categorías como DTOs.</summary>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Lista de categorías (DTOs, no entidades).</returns>
    public async Task<IReadOnlyList<CategoriaDto>> GetCategorias(
        [Service] ICategoriaRepository categoriaRepository) {
        IEnumerable<Categoria> categorias = await categoriaRepository.FindAllAsync();
        return categorias.Select(c => c.ToDto()).ToList();
    }

    /// <summary>Obtiene una categoría por ID como DTO.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Categoría encontrada (DTO) o null.</returns>
    public async Task<CategoriaDto?> GetCategoria(
        long id,
        [Service] ICategoriaRepository categoriaRepository) {
        Categoria? categoria = await categoriaRepository.FindByIdAsync(id);
        return categoria?.ToDto();
    }

    /// <summary>Obtiene todas las categorías paginadas.</summary>
    /// <param name="page">Número de página (base 1, contrato GraphQL).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Resultado paginado de categorías.</returns>
    public async Task<PagedResult<CategoriaDto>> GetCategoriasPaged(
        [Service] ICategoriaRepository categoriaRepository,
        int page = 1,
        int size = 10) {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0 (Skip(Page*Size))
        var filter = new CategoriaFilterDto {
            Nombre = null,
            Page = Math.Max(page - 1, 0),
            Size = size
        };
        (IEnumerable<Categoria> Items, int TotalCount) result = await categoriaRepository.FindAllPagedAsync(filter);
        return new PagedResult<CategoriaDto> {
            Items = result.Items.Select(c => new CategoriaDto(c.Id, c.Nombre, c.Descripcion, c.CreatedAt, c.UpdatedAt)),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = size
        };
    }

    // ════════════════════════ PEDIDOS (solo queries) ════════════════════════
    // 🎓 Decisión de diseño: GraphQL solo LEE pedidos (sin mutations ni
    // subscriptions): crear/editar/borrar es cosa de REST. El control de
    // acceso es de dos niveles:
    //   · pedidos      → solo ADMIN (lista completa)
    //   · misPedidos   → cualquier autenticado, FILTRADO por su propio claim
    //                     (un usuario jamás ve pedidos ajenos por GraphQL).

    /// <summary>
    ///     Obtiene TODOS los pedidos. Solo administradores.
    /// </summary>
    /// <param name="pedidosRepository">Repositorio de pedidos.</param>
    /// <returns>Lista de pedidos (DTOs, no entidades).</returns>
    [Authorize("AdminOnly")]
    public async Task<IReadOnlyList<PedidoDto>> GetPedidos(
        [Service] IPedidosRepository pedidosRepository) {
        IEnumerable<Pedido> pedidos = await pedidosRepository.FindAllAsync();
        return pedidos.Select(p => p.ToDto()).ToList();
    }

    /// <summary>
    ///     Obtiene MIS pedidos: los del usuario autenticado (cualquier usuario
    ///     logueado). El userId sale del claim del JWT (inyectado por
    ///     HotChocolate como parámetro) y el repositorio filtra por él: nunca se
    ///     devuelven pedidos de otros usuarios.
    /// </summary>
    /// <param name="user">Usuario autenticado (claims del JWT).</param>
    /// <param name="pedidosRepository">Repositorio de pedidos.</param>
    /// <returns>Lista de pedidos del usuario autenticado.</returns>
    [Authorize]
    public async Task<IReadOnlyList<PedidoDto>> GetMisPedidos(
        ClaimsPrincipal user,
        [Service] IPedidosRepository pedidosRepository) {
        string? claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim is null || !long.TryParse(claim, out long userId)) {
            throw new GraphQLException(
                "No se pudo identificar al usuario autenticado (falta el claim NameIdentifier).");
        }

        IEnumerable<Pedido> pedidos = await pedidosRepository.FindByUserIdAsync(userId);
        return pedidos.Select(p => p.ToDto()).ToList();
    }
}
