using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Categorias;

/// <summary>
/// Implementación del repositorio de categorías.
/// </summary>
public class CategoriaRepository(
    TiendaDbContext context,
    ILogger<CategoriaRepository> logger
) : ICategoriaRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Categoria>> FindAllAsync()
    {
        logger.LogDebug("Buscando todas las categorías");
        return await context.Categorias
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public IQueryable<Categoria> FindAllAsNoTracking()
    {
        logger.LogDebug("Obteniendo categorías como IQueryable");
        return context.Categorias
            .OrderBy(c => c.Nombre)
            .AsNoTracking();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Categoria> Items, int TotalCount)> FindAllPagedAsync(CategoriaFilterDto filter)
    {
        logger.LogDebug("Buscando categorías paginadas con filtros");

        var query = context.Categorias.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            query = query.Where(c => c.Nombre.Contains(filter.Nombre));

        if (filter.IsDeleted.HasValue)
        {
            query = query.IgnoreQueryFilters();
            query = query.Where(c => c.IsDeleted == filter.IsDeleted.Value);
        }

        var totalCount = await query.CountAsync();

        var orderedQuery = filter.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(GetSortExpression(filter.SortBy))
            : query.OrderBy(GetSortExpression(filter.SortBy));

        var items = await orderedQuery
            .Skip(filter.Page * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<Categoria?> FindByIdAsync(long id)
    {
        logger.LogDebug("Buscando categoría por ID: {Id}", id);
        return await context.Categorias.FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIdAsync(long id)
    {
        return await context.Categorias.AnyAsync(c => c.Id == id);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNombreAsync(string nombre, long? excludeId = null)
    {
        var query = context.Categorias.Where(c => c.Nombre == nombre);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <inheritdoc/>
    public async Task<Categoria> CreateAsync(Categoria categoria)
    {
        logger.LogDebug("Creando categoría: {Nombre}", categoria.Nombre);
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    /// <inheritdoc/>
    public async Task<Categoria> UpdateAsync(Categoria categoria)
    {
        logger.LogDebug("Actualizando categoría ID: {Id}", categoria.Id);
        context.Categorias.Update(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id)
    {
        logger.LogDebug("Eliminando categoría ID: {Id}", id);
        var categoria = await FindByIdAsync(id);
        if (categoria == null) return false;

        categoria.IsDeleted = true;
        await UpdateAsync(categoria);
        return true;
    }

    /// <inheritdoc/>
    public async Task<Categoria> SaveAsync(Categoria categoria)
    {
        logger.LogDebug("Guardando categoría: {Nombre}", categoria.Nombre);
        var existing = await FindByIdAsync(categoria.Id);
        if (existing != null)
        {
            context.Entry(existing).CurrentValues.SetValues(categoria);
        }
        else
        {
            context.Categorias.Add(categoria);
        }
        await context.SaveChangesAsync();
        return categoria;
    }

    private static Expression<Func<Categoria, object>> GetSortExpression(string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "nombre" => c => c.Nombre,
            "createdat" => c => c.CreatedAt,
            _ => c => c.Id
        };
    }
}
