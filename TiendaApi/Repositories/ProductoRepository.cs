using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models.Entities;

namespace TiendaApi.Repositories;

/// <summary>
/// Implementation of Producto repository using Entity Framework Core
/// </summary>
public class ProductoRepository : IProductoRepository
{
    private readonly TiendaDbContext _context;

    public ProductoRepository(TiendaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Producto>> FindAllAsync()
    {
        return await _context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Producto?> FindByIdAsync(long id)
    {
        return await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId)
    {
        return await _context.Productos
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaId == categoriaId)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Producto> SaveAsync(Producto producto)
    {
        producto.CreatedAt = DateTime.UtcNow;
        producto.UpdatedAt = DateTime.UtcNow;
        
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
        
        // Reload with Categoria
        await _context.Entry(producto)
            .Reference(p => p.Categoria)
            .LoadAsync();
        
        return producto;
    }

    public async Task<Producto> UpdateAsync(Producto producto)
    {
        producto.UpdatedAt = DateTime.UtcNow;
        
        _context.Productos.Update(producto);
        await _context.SaveChangesAsync();
        
        // Reload with Categoria
        await _context.Entry(producto)
            .Reference(p => p.Categoria)
            .LoadAsync();
        
        return producto;
    }

    public async Task DeleteAsync(long id)
    {
        var producto = await FindByIdAsync(id);
        if (producto != null)
        {
            producto.IsDeleted = true;
            producto.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _context.Productos.AnyAsync(p => p.Id == id);
    }
}
