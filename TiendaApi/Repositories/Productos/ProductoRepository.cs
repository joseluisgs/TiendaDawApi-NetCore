using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models;

namespace TiendaApi.Repositories.Productos;

/// <summary>
/// Implementación del repositorio de productos usando Entity Framework Core.
/// </summary>
public class ProductoRepository(
    TiendaDbContext context,
    ILogger<ProductoRepository> logger
) : IProductoRepository {

    /// <summary>
    /// Obtiene todos los productos ordenados por nombre.
    /// </summary>
    /// <returns>Colección de productos con su categoría incluida.</returns>
    public async Task<IEnumerable<Producto>> FindAllAsync() {
        logger.LogDebug("Buscando todos los productos");
        
        return await context.Productos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un producto por su identificador.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>El producto encontrado con su categoría o null.</returns>
    public async Task<Producto?> FindByIdAsync(long id) {
        return await context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Obtiene productos por identificador de categoría.
    /// </summary>
    /// <param name="categoriaId">Identificador de la categoría.</param>
    /// <returns>Colección de productos de la categoría ordenada por nombre.</returns>
    public async Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId) {
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
    public async Task<Producto> SaveAsync(Producto producto) {
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
    public async Task<Producto> UpdateAsync(Producto producto) {
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
    public async Task DeleteAsync(long id) {
        var producto = await FindByIdAsync(id);
        if (producto != null) {
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
    public async Task<bool> ExistsAsync(long id) {
        return await context.Productos.AnyAsync(p => p.Id == id);
    }
}
