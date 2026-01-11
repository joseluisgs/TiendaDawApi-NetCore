using TiendaApi.Models;

namespace TiendaApi.Repositories.Productos;

/// <summary>
/// Interfaz del repositorio de productos.
/// </summary>
public interface IProductoRepository
{
    /// <summary>
    /// Obtiene todos los productos ordenados por nombre.
    /// </summary>
    /// <returns>Colección de productos.</returns>
    Task<IEnumerable<Producto>> FindAllAsync();

    /// <summary>
    /// Obtiene un producto por su identificador.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>El producto encontrado o null si no existe.</returns>
    Task<Producto?> FindByIdAsync(long id);

    /// <summary>
    /// Obtiene productos por identificador de categoría.
    /// </summary>
    /// <param name="categoriaId">Identificador de la categoría.</param>
    /// <returns>Colección de productos de la categoría.</returns>
    Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>
    /// Guarda un nuevo producto.
    /// </summary>
    /// <param name="producto">Producto a guardar.</param>
    /// <returns>El producto guardado con los datos actualizados.</returns>
    Task<Producto> SaveAsync(Producto producto);

    /// <summary>
    /// Actualiza un producto existente.
    /// </summary>
    /// <param name="producto">Producto con los datos actualizados.</param>
    /// <returns>El producto actualizado.</returns>
    Task<Producto> UpdateAsync(Producto producto);

    /// <summary>
    /// Elimina un producto por su identificador (eliminación suave).
    /// </summary>
    /// <param name="id">Identificador del producto a eliminar.</param>
    /// <returns>Tarea asíncrona.</returns>
    Task DeleteAsync(long id);

    /// <summary>
    /// Verifica si existe un producto con el identificador especificado.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>True si existe, False en caso contrario.</returns>
    Task<bool> ExistsAsync(long id);
}
