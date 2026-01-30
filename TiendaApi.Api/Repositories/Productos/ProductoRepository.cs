using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Productos;

/// <inheritdoc cref="IProductoRepository" />
public class ProductoRepository(
    TiendaDbContext context,
    ILogger<ProductoRepository> logger
) : IProductoRepository
{
    /// <inheritdoc cref="IProductoRepository.FindAllAsync" />
    public async Task<IEnumerable<Producto>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los productos");
        return await context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc cref="IProductoRepository.FindAllAsNoTracking" />
    public IQueryable<Producto> FindAllAsNoTracking()
    {
        logger.LogDebug("Obteniendo productos como IQueryable");
        return context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .AsNoTracking();
    }

    /// <inheritdoc cref="IProductoRepository.FindAllPagedAsync(ProductoFilterDto)" />
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

    /// <inheritdoc cref="IProductoRepository.FindByIdAsync(long)" />
    public async Task<Producto?> FindByIdAsync(long id)
    {
        return await context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <inheritdoc cref="IProductoRepository.FindByCategoriaIdAsync(long)" />
    public async Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId)
    {
        logger.LogDebug("Buscando productos para categoría: {CategoriaId}", categoriaId);
        return await context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaId == categoriaId)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc cref="IProductoRepository.SaveAsync(Producto)" />
    public async Task<Producto> SaveAsync(Producto producto)
    {
        context.Productos.Add(producto);
        await context.SaveChangesAsync();
        await context.Entry(producto).Reference(p => p.Categoria).LoadAsync();
        logger.LogInformation("Producto guardado con ID: {Id}", producto.Id);
        return producto;
    }

    /// <inheritdoc cref="IProductoRepository.UpdateAsync(Producto)" />
    public async Task<Producto> UpdateAsync(Producto producto)
    {
        context.Productos.Update(producto);
        await context.SaveChangesAsync();
        await context.Entry(producto).Reference(p => p.Categoria).LoadAsync();
        logger.LogInformation("Producto actualizado con ID: {Id}", producto.Id);
        return producto;
    }

    /// <inheritdoc cref="IProductoRepository.DeleteAsync(long)" />
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

    /// <inheritdoc cref="IProductoRepository.ExistsAsync(long)" />
    public async Task<bool> ExistsAsync(long id)
    {
        return await context.Productos.AnyAsync(p => p.Id == id);
    }

    /// <inheritdoc cref="IProductoRepository.DecrementStockAsync(long, int, byte[])" />
    public async Task<bool> DecrementStockAsync(long productoId, int cantidad, byte[] expectedRowVersion)
    {
        logger.LogDebug("Decrementando stock para producto: {ProductoId}, cantidad: {Cantidad}", productoId, cantidad);
        var producto = await context.Productos.FindAsync(productoId);

        if (producto is null || producto.Stock < cantidad) return false;

        producto.Stock -= cantidad;
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Conflicto de concurrencia al decrementar stock");
            throw;
        }
    }

    /// <inheritdoc cref="IProductoRepository.BeginTransactionAsync(IsolationLevel)" />
    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
    {
        return await context.Database.BeginTransactionAsync(isolationLevel);
    }

    /// <inheritdoc cref="IProductoRepository.GetRecentlyCreatedAsync(int)" />
    public async Task<IEnumerable<Producto>> GetRecentlyCreatedAsync(int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);
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