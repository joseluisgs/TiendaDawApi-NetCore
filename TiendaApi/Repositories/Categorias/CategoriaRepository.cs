using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models;
using TiendaApi.Repositories.Categorias;

namespace TiendaApi.Repositories.Categorias;

/// <summary>
/// Implementación del repositorio de categorías usando Entity Framework Core.
/// </summary>
public class CategoriaRepository : ICategoriaRepository
{
    private readonly TiendaDbContext _context;

    public CategoriaRepository(TiendaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene todas las categorías ordenadas por nombre.
    /// </summary>
    /// <returns>Colección de categorías ordenadas.</returns>
    public async Task<IEnumerable<Categoria>> FindAllAsync()
    {
        return await _context.Categorias
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
        return await _context.Categorias
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
        
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();
        
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
        
        _context.Categorias.Update(categoria);
        await _context.SaveChangesAsync();
        
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
            await _context.SaveChangesAsync();
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
        var query = _context.Categorias.Where(c => c.Nombre == nombre);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        return await query.AnyAsync();
    }
}
