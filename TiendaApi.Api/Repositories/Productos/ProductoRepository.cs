using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Productos;

/// <summary>
/// Implementación del repositorio de productos.
/// </summary>
public class ProductoRepository(
    TiendaDbContext context,
    ILogger<ProductoRepository> logger
) : IProductoRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Producto>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los productos");
        return await context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public IQueryable<Producto> FindAllAsNoTracking()
    {
        logger.LogDebug("Obteniendo productos como IQueryable");
        return context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .AsNoTracking();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Producto> Items, int TotalCount)> FindAllPagedAsync(ProductoFilterDto filter)
    {
        logger.LogDebug("Buscando productos paginados con filtros");

        var query = filter.IsDeleted.HasValue
            ? context.Productos.IgnoreQueryFilters().Include(p => p.Categoria).AsQueryable()
            : context.Productos.Include(p => p.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            query = query.Where(p => EF.Functions.Like(p.Nombre, $"%{filter.Nombre}%"));

        if (!string.IsNullOrWhiteSpace(filter.Categoria))
            query = query.Where(p => EF.Functions.Like(p.Categoria.Nombre, $"%{filter.Categoria}%"));

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

    /// <inheritdoc/>
    public async Task<Producto?> FindByIdAsync(long id)
    {
        return await context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId)
    {
        logger.LogDebug("Buscando productos para categoría: {CategoriaId}", categoriaId);
        return await context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaId == categoriaId)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Producto> SaveAsync(Producto producto)
    {
        context.Productos.Add(producto);
        await context.SaveChangesAsync();
        await context.Entry(producto).Reference(p => p.Categoria).LoadAsync();
        logger.LogInformation("Producto guardado con ID: {Id}", producto.Id);
        return producto;
    }

    /// <inheritdoc/>
    public async Task<Producto> UpdateAsync(Producto producto)
    {
        context.Productos.Update(producto);
        await context.SaveChangesAsync();
        await context.Entry(producto).Reference(p => p.Categoria).LoadAsync();
        logger.LogInformation("Producto actualizado con ID: {Id}", producto.Id);
        return producto;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(long id)
    {
        var producto = await FindByIdAsync(id);
        if (producto is not null)
        {
            producto.IsDeleted = true;
            await context.SaveChangesAsync();
            logger.LogInformation("Producto eliminado lógicamente con ID: {Id}", id);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(long id)
    {
        return await context.Productos.AnyAsync(p => p.Id == id);
    }

    /// <inheritdoc/>
    public async Task<bool> DecrementStockAsync(long productoId, int cantidad, byte[] expectedRowVersion)
    {
        logger.LogDebug("Decrementando stock para producto: {ProductoId}, cantidad: {Cantidad}", productoId, cantidad);
        var producto = await context.Productos.FindAsync(productoId);

        if (producto is null)
        {
            logger.LogWarning("Producto no encontrado: {ProductoId}", productoId);
            return false;
        }

        if (producto.Stock < cantidad)
        {
            logger.LogWarning("Stock insuficiente. Actual: {Stock}, Solicitado: {Cantidad}", producto.Stock, cantidad);
            return false;
        }

        producto.Stock -= cantidad;

        try
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Stock decrementado. Nuevo stock: {Stock}", producto.Stock);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Conflicto de concurrencia al decrementar stock: {ProductoId}", productoId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
        logger.LogDebug("Transacción iniciada con nivel: {IsolationLevel}", isolationLevel);
        return transaction;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Producto>> GetRecentlyCreatedAsync(int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        logger.LogDebug("Buscando productos desde: {Since}", since);
        return await context.Productos
            .Where(p => p.CreatedAt >= since && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
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
            "categoria" => p => p.Categoria.Nombre,
            _ => p => p.Id
        };
        return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
