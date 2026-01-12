using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Productos;

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

    /// <summary>
    /// Decrementa el stock de un producto atómicamente usando control de concurrencia optimista.
    /// Genera DbUpdateConcurrencyException si el RowVersion no coincide.
    /// </summary>
    /// <param name="productoId">Identificador del producto.</param>
    /// <param name="cantidad">Cantidad a decrementar del stock.</param>
    /// <param name="expectedRowVersion">Versión esperada del registro (para control de concurrencia).</param>
    /// <returns>True si el stock fue decrementado, False si el producto no existe.</returns>
    Task<bool> DecrementStockAsync(long productoId, int cantidad, byte[] expectedRowVersion);
}
