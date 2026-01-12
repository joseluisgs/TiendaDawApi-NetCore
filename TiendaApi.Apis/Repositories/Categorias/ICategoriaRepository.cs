using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Categorias;

/// <summary>
/// Interfaz del repositorio de categorías.
/// </summary>
public interface ICategoriaRepository
{
    /// <summary>
    /// Obtiene todas las categorías ordenadas por nombre.
    /// </summary>
    /// <returns>Colección de categorías.</returns>
    Task<IEnumerable<Categoria>> FindAllAsync();

    /// <summary>
    /// Obtiene una categoría por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <returns>La categoría encontrada o null si no existe.</returns>
    Task<Categoria?> FindByIdAsync(long id);

    /// <summary>
    /// Guarda una nueva categoría.
    /// </summary>
    /// <param name="categoria">Categoría a guardar.</param>
    /// <returns>La categoría guardada con los datos actualizados.</returns>
    Task<Categoria> SaveAsync(Categoria categoria);

    /// <summary>
    /// Actualiza una categoría existente.
    /// </summary>
    /// <param name="categoria">Categoría con los datos actualizados.</param>
    /// <returns>La categoría actualizada.</returns>
    Task<Categoria> UpdateAsync(Categoria categoria);

    /// <summary>
    /// Elimina una categoría por su identificador (eliminación suave).
    /// </summary>
    /// <param name="id">Identificador de la categoría a eliminar.</param>
    /// <returns>Tarea asíncrona.</returns>
    Task DeleteAsync(long id);

    /// <summary>
    /// Verifica si existe una categoría con el nombre especificado.
    /// </summary>
    /// <param name="nombre">Nombre de la categoría a buscar.</param>
    /// <param name="excludeId">Identificador a excluir de la búsqueda (para actualizaciones).</param>
    /// <returns>True si existe, False en caso contrario.</returns>
    Task<bool> ExistsByNombreAsync(string nombre, long? excludeId = null);
}
