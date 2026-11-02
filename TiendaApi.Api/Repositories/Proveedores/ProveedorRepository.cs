using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Proveedores;

/// <summary>
/// Implementación del repositorio de proveedores sobre PostgreSQL.
/// </summary>
public class ProveedorRepository(
    TiendaDbContext context,
    ILogger<ProveedorRepository> logger
) : IProveedorRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Proveedor>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los proveedores");
        return await context.Proveedores
            .OrderBy(p => p.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public IQueryable<Proveedor> FindAllAsNoTracking()
    {
        logger.LogDebug("Obteniendo proveedores como IQueryable");
        return context.Proveedores
            .OrderBy(p => p.Nombre)
            .AsNoTracking();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Proveedor> Items, int TotalCount)> FindAllPagedAsync(ProveedorFilterDto filter)
    {
        logger.LogDebug("Buscando proveedores paginados con filtros");

        var query = context.Proveedores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            query = query.Where(p => p.Nombre.Contains(filter.Nombre));

        if (filter.IsDeleted.HasValue)
        {
            query = query.IgnoreQueryFilters();
            query = query.Where(p => p.IsDeleted == filter.IsDeleted.Value);
        }

        var totalCount = await query.CountAsync();

        var orderedQuery = filter.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(GetSortExpression(filter.SortBy))
            : query.OrderBy(GetSortExpression(filter.SortBy));

        var size = Math.Clamp(filter.Size, 1, 100);
        var page = Math.Max(filter.Page, 0);

        var items = await orderedQuery
            .AsNoTracking()
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<Proveedor?> FindByIdAsync(string codigo)
    {
        logger.LogDebug("Buscando proveedor por código: {Codigo}", codigo);
        return await context.Proveedores.FirstOrDefaultAsync(p => p.Codigo == codigo);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIdAsync(string codigo)
    {
        return await context.Proveedores.AnyAsync(p => p.Codigo == codigo);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNombreAsync(string nombre, string? excludeCodigo = null)
    {
        var query = context.Proveedores.Where(p => p.Nombre == nombre);
        if (!string.IsNullOrEmpty(excludeCodigo))
            query = query.Where(p => p.Codigo != excludeCodigo);
        return await query.AnyAsync();
    }

    /// <inheritdoc/>
    public async Task<int> CountProductosAsync(string codigo)
    {
        // Los productos tienen filtro global de borrado lógico: solo se cuentan los activos.
        return await context.Productos.CountAsync(p => p.ProveedorId == codigo);
    }

    /// <inheritdoc/>
    public async Task<Proveedor> CreateAsync(Proveedor proveedor)
    {
        logger.LogDebug("Creando proveedor: {Nombre}", proveedor.Nombre);
        context.Proveedores.Add(proveedor);
        await context.SaveChangesAsync();
        return proveedor;
    }

    /// <inheritdoc/>
    public async Task<Proveedor> UpdateAsync(Proveedor proveedor)
    {
        logger.LogDebug("Actualizando proveedor código: {Codigo}", proveedor.Codigo);
        context.Proveedores.Update(proveedor);
        await context.SaveChangesAsync();
        return proveedor;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string codigo)
    {
        logger.LogDebug("Eliminando proveedor código: {Codigo}", codigo);
        var proveedor = await FindByIdAsync(codigo);
        if (proveedor == null) return false;

        proveedor.IsDeleted = true;
        await UpdateAsync(proveedor);
        return true;
    }

    private static Expression<Func<Proveedor, object>> GetSortExpression(string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "nombre" => p => p.Nombre,
            "createdat" => p => p.CreatedAt,
            _ => p => p.Codigo
        };
    }
}
