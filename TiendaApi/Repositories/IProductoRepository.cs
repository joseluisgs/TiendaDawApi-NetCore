using TiendaApi.Models.Entities;

namespace TiendaApi.Repositories;

/// <summary>
/// Repository interface for Producto
/// </summary>
public interface IProductoRepository
{
    Task<IEnumerable<Producto>> FindAllAsync();
    Task<Producto?> FindByIdAsync(long id);
    Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId);
    Task<Producto> SaveAsync(Producto producto);
    Task<Producto> UpdateAsync(Producto producto);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
}
