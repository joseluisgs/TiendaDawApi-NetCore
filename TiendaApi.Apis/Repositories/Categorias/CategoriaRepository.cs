using Microsoft.EntityFrameworkCore;
using TiendaApi.Apis.Data;
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
        categoria.CreatedAt = DateTime.UtcNow;
        categoria.UpdatedAt = DateTime.UtcNow;

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
        categoria.UpdatedAt = DateTime.UtcNow;

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
        if (categoria != null)
        {
            categoria.IsDeleted = true;
            categoria.UpdatedAt = DateTime.UtcNow;
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
