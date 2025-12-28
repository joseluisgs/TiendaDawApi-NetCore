using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models.Entities;

namespace TiendaApi.Repositories;

/// <summary>
/// Implementation of Categoria repository using Entity Framework Core
/// 
/// Java equivalent: @Repository class implementing JpaRepository
/// Uses DbContext similar to how Spring uses EntityManager
/// </summary>
public class CategoriaRepository : ICategoriaRepository
{
    private readonly TiendaDbContext _context;

    public CategoriaRepository(TiendaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Categoria>> FindAllAsync()
    {
        return await _context.Categorias
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Categoria?> FindByIdAsync(long id)
    {
        return await _context.Categorias
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Categoria> SaveAsync(Categoria categoria)
    {
        categoria.CreatedAt = DateTime.UtcNow;
        categoria.UpdatedAt = DateTime.UtcNow;
        
        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();
        
        return categoria;
    }

    public async Task<Categoria> UpdateAsync(Categoria categoria)
    {
        categoria.UpdatedAt = DateTime.UtcNow;
        
        _context.Categorias.Update(categoria);
        await _context.SaveChangesAsync();
        
        return categoria;
    }

    public async Task DeleteAsync(long id)
    {
        var categoria = await FindByIdAsync(id);
        if (categoria != null)
        {
            // Soft delete
            categoria.IsDeleted = true;
            categoria.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

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
