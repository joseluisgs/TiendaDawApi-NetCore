using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Categorias;

/// <summary>
/// Implementación del repositorio de categorías usando Entity Framework Core.
/// </summary>
public class CategoriaRepository(
    TiendaDbContext context,
    ILogger<CategoriaRepository> logger
) : ICategoriaRepository
{

    /// <summary>
    /// Obtiene todas las categorías ordenadas por nombre.
    /// </summary>
    /// <returns>Colección de categorías ordenadas.</returns>
    public async Task<IEnumerable<Categoria>> FindAllAsync()
    {
        logger.LogDebug("Buscando todas las categorías");

        return await context.Categorias
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene todas las categorías como IQueryable para uso con HotChocolate.
    /// </summary>
    /// <returns>IQueryable de categorías.</returns>
    public IQueryable<Categoria> FindAllAsNoTracking()
    {
        logger.LogDebug("Obteniendo categorías como IQueryable para HotChocolate");

        return context.Categorias
            .OrderBy(c => c.Nombre)
            .AsNoTracking();
    }

    /// <summary>
    /// Obtiene categorías paginadas con filtros opcionales.
    /// </summary>
    public async Task<(IEnumerable<Categoria> Items, int TotalCount)> FindAllPagedAsync(CategoriaFilterDto filter)
    {
        logger.LogDebug("Buscando categorías paginadas con filtros");

        var query = filter.IsDeleted.HasValue
            ? context.Categorias.IgnoreQueryFilters().AsQueryable()
            : context.Categorias.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            query = query.Where(c => EF.Functions.Like(c.Nombre, $"%{filter.Nombre}%"));

        if (filter.IsDeleted.HasValue)
            query = query.Where(c => c.IsDeleted == filter.IsDeleted.Value);

        var totalCount = await query.CountAsync();

        query = ApplySorting(query, filter.SortBy, filter.Direction);

        var items = await query
            .Skip(filter.Page * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

        return (items, totalCount);
    }

    private static IQueryable<Categoria> ApplySorting(IQueryable<Categoria> query, string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        Expression<Func<Categoria, object>> keySelector = sortBy.ToLower() switch
        {
            "nombre" => c => c.Nombre,
            "createdat" => c => c.CreatedAt,
            "updatedat" => c => c.UpdatedAt,
            _ => c => c.Id
        };

        return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    /// <summary>
    /// Obtiene una categoría por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <returns>La categoría encontrada o null.</returns>
    public async Task<Categoria?> FindByIdAsync(long id)
    {
        return await context.Categorias
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Guarda una nueva categoría.
    /// </summary>
    /// <param name="categoria">Categoría a guardar.</param>
    /// <returns>La categoría guardada con fecha de creación y modificación.</returns>
    public async Task<Categoria> SaveAsync(Categoria categoria)
    {
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();

        logger.LogInformation("Categoría guardada con ID: {Id}", categoria.Id);

        return categoria;
    }

    /// <summary>
    /// Actualiza una categoría existente.
    /// </summary>
    /// <param name="categoria">Categoría con datos actualizados.</param>
    /// <returns>La categoría actualizada.</returns>
    public async Task<Categoria> UpdateAsync(Categoria categoria)
    {
        context.Categorias.Update(categoria);
        await context.SaveChangesAsync();

        logger.LogInformation("Categoría actualizada con ID: {Id}", categoria.Id);

        return categoria;
    }

    /// <summary>
    /// Elimina una categoría por su identificador (eliminación suave).
    /// </summary>
    /// <param name="id">Identificador de la categoría a eliminar.</param>
    /// <returns>Tarea asíncrona.</returns>
    public async Task DeleteAsync(long id)
    {
        var categoria = await FindByIdAsync(id);
        if (categoria is not null)
        {
            categoria.IsDeleted = true;
            await context.SaveChangesAsync();

            logger.LogInformation("Categoría eliminada lógicamente con ID: {Id}", id);
        }
    }

    /// <summary>
    /// Verifica si existe una categoría con el nombre especificado.
    /// </summary>
    /// <param name="nombre">Nombre de la categoría.</param>
    /// <param name="excludeId">Identificador a excluir de la búsqueda.</param>
    /// <returns>True si existe, False en caso contrario.</returns>
    public async Task<bool> ExistsByNombreAsync(string nombre, long? excludeId = null)
    {
        var query = context.Categorias.Where(c => c.Nombre == nombre);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
