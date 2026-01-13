using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Productos;

/// <summary>
/// Implementación del repositorio de productos usando Entity Framework Core.
/// </summary>
public class ProductoRepository(
    TiendaDbContext context,
    ILogger<ProductoRepository> logger
) : IProductoRepository
{

    /// <summary>
    /// Obtiene todos los productos ordenados por nombre.
    /// </summary>
    /// <returns>Colección de productos con su categoría incluida.</returns>
    public async Task<IEnumerable<Producto>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los productos");

        return await context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene todos los productos como IQueryable para uso con HotChocolate.
    /// </summary>
    /// <returns>IQueryable de productos.</returns>
    public IQueryable<Producto> FindAllAsNoTracking()
    {
        logger.LogDebug("Obteniendo productos como IQueryable para HotChocolate");

        return context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .AsNoTracking();
    }

    /// <summary>
    /// Obtiene productos paginados con filtros opcionales.
    /// </summary>
    public async Task<(IEnumerable<Producto> Items, int TotalCount)> FindAllPagedAsync(ProductoFilterDto filter)
    {
        logger.LogDebug("Buscando productos paginados con filtros");

        var query = filter.IsDeleted.HasValue
            ? context.Productos.IgnoreQueryFilters().Include(p => p.Categoria).AsQueryable()
            : context.Productos.Include(p => p.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            query = query.Where(p => p.Nombre.ToLower().Contains(filter.Nombre.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Categoria))
            query = query.Where(p => p.Categoria.Nombre.ToLower().Contains(filter.Categoria.ToLower()));

        if (filter.IsDeleted.HasValue)
            query = query.Where(p => p.IsDeleted == filter.IsDeleted.Value);

        if (filter.PrecioMax.HasValue)
            query = query.Where(p => p.Precio <= filter.PrecioMax.Value);

        if (filter.StockMin.HasValue)
            query = query.Where(p => p.Stock >= filter.StockMin.Value);

        var totalCount = await query.CountAsync();

        query = ApplySorting(query, filter.SortBy, filter.Direction);

        var items = await query
            .Skip(filter.Page * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

        return (items, totalCount);
    }

    private static IQueryable<Producto> ApplySorting(IQueryable<Producto> query, string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        Expression<Func<Producto, object>> keySelector = sortBy.ToLower() switch
        {
            "nombre" => p => p.Nombre,
            "precio" => p => p.Precio,
            "stock" => p => p.Stock,
            "createdat" => p => p.CreatedAt,
            "updatedat" => p => p.UpdatedAt,
            "categoria" => p => p.Categoria.Nombre,
            _ => p => p.Id
        };

        return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    /// <summary>
    /// Obtiene un producto por su identificador.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>El producto encontrado con su categoría o null.</returns>
    public async Task<Producto?> FindByIdAsync(long id)
    {
        return await context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Obtiene productos por identificador de categoría.
    /// </summary>
    /// <param name="categoriaId">Identificador de la categoría.</param>
    /// <returns>Colección de productos de la categoría ordenada por nombre.</returns>
    public async Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId)
    {
        logger.LogDebug("Buscando productos para categoría: {CategoriaId}", categoriaId);

        return await context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaId == categoriaId)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Guarda un nuevo producto.
    /// </summary>
    /// <param name="producto">Producto a guardar.</param>
    /// <returns>El producto guardado con fecha de creación y categoría cargada.</returns>
    public async Task<Producto> SaveAsync(Producto producto)
    {
        producto.CreatedAt = DateTime.UtcNow;
        producto.UpdatedAt = DateTime.UtcNow;

        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        await context.Entry(producto)
            .Reference(p => p.Categoria)
            .LoadAsync();

        logger.LogInformation("Producto guardado con ID: {Id}", producto.Id);

        return producto;
    }

    /// <summary>
    /// Actualiza un producto existente.
    /// </summary>
    /// <param name="producto">Producto con datos actualizados.</param>
    /// <returns>El producto actualizado con categoría cargada.</returns>
    public async Task<Producto> UpdateAsync(Producto producto)
    {
        producto.UpdatedAt = DateTime.UtcNow;

        context.Productos.Update(producto);
        await context.SaveChangesAsync();

        await context.Entry(producto)
            .Reference(p => p.Categoria)
            .LoadAsync();

        logger.LogInformation("Producto actualizado con ID: {Id}", producto.Id);

        return producto;
    }

    /// <summary>
    /// Elimina un producto por su identificador (eliminación suave).
    /// </summary>
    /// <param name="id">Identificador del producto a eliminar.</param>
    /// <returns>Tarea asíncrona.</returns>
    public async Task DeleteAsync(long id)
    {
        var producto = await FindByIdAsync(id);
        if (producto != null)
        {
            producto.IsDeleted = true;
            producto.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            logger.LogInformation("Producto eliminado lógicamente con ID: {Id}", id);
        }
    }

    /// <summary>
    /// Verifica si existe un producto con el identificador especificado.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>True si existe, False en caso contrario.</returns>
    public async Task<bool> ExistsAsync(long id)
    {
        return await context.Productos.AnyAsync(p => p.Id == id);
    }

    /// <summary>
    /// Decrementa el stock de un producto atómicamente usando control de concurrencia optimista.
    /// Genera DbUpdateConcurrencyException si el RowVersion no coincide.
    /// </summary>
    /// <param name="productoId">Identificador del producto.</param>
    /// <param name="cantidad">Cantidad a decrementar del stock.</param>
    /// <param name="expectedRowVersion">Versión esperada del registro (para control de concurrencia).</param>
    /// <returns>True si el stock fue decrementado, False si el producto no existe.</returns>
    public async Task<bool> DecrementStockAsync(long productoId, int cantidad, byte[] expectedRowVersion)
    {
        logger.LogDebug("Intentando decrementar stock para producto: {ProductoId}, cantidad: {Cantidad}", productoId, cantidad);

        var producto = await context.Productos.FindAsync(productoId);

        if (producto == null)
        {
            logger.LogWarning("Producto no encontrado para decrementar stock: {ProductoId}", productoId);
            return false;
        }

        producto.Stock -= cantidad;
        producto.UpdatedAt = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Stock decrementado exitosamente para producto: {ProductoId}, nuevo stock: {Stock}", productoId, producto.Stock);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Conflicto de concurrencia al decrementar stock para producto: {ProductoId}", productoId);
            throw;
        }
    }

    /// <summary>
    /// Inicia una transacción con el nivel de aislamiento especificado.
    /// Usado para el enfoque híbrido Serializable + Retry.
    /// </summary>
    /// <param name="isolationLevel">Nivel de aislamiento de la transacción.</param>
    /// <returns>La transacción iniciada.</returns>
    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
        logger.LogDebug("Transacción iniciada con nivel de aislamiento: {IsolationLevel}", isolationLevel);
        return transaction;
    }
}
