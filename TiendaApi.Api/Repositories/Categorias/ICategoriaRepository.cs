using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Categorias;

/// <summary>
///     Contrato del repositorio de categorías.
/// </summary>
public interface ICategoriaRepository {
    /// <summary>Obtiene todas las categorías ordenadas por nombre.</summary>
    /// <returns>Colección de categorías.</returns>
    Task<IEnumerable<Categoria>> FindAllAsync();

    /// <summary>Obtiene categorías como IQueryable para GraphQL.</summary>
    /// <returns>IQueryable de categorías.</returns>
    IQueryable<Categoria> FindAllAsNoTracking();

    /// <summary>Obtiene categorías paginadas con filtros.</summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Tupla con items y total de registros.</returns>
    Task<(IEnumerable<Categoria> Items, int TotalCount)> FindAllPagedAsync(CategoriaFilterDto filter);

    /// <summary>Busca una categoría por su ID.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>Categoría encontrada o null.</returns>
    Task<Categoria?> FindByIdAsync(long id);

    /// <summary>Verifica si existe una categoría por ID.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>True si existe.</returns>
    Task<bool> ExistsByIdAsync(long id);

    /// <summary>Verifica si existe una categoría por nombre.</summary>
    /// <param name="nombre">Nombre de la categoría.</param>
    /// <param name="excludeId">ID a excluir (para actualizaciones).</param>
    /// <returns>True si existe.</returns>
    Task<bool> ExistsByNombreAsync(string nombre, long? excludeId = null);

    /// <summary>Crea una nueva categoría.</summary>
    /// <param name="categoria">Categoría a crear.</param>
    /// <returns>Categoría creada.</returns>
    Task<Categoria> CreateAsync(Categoria categoria);

    /// <summary>
    ///     Crea una categoría con un ID concreto, para el restore de un backup.
    ///     La columna de identidad es <c>GENERATED ALWAYS</c>: requiere
    ///     <c>OVERRIDING SYSTEM VALUE</c> y dejar la secuencia al día.
    /// </summary>
    /// <param name="categoria">Categoría con su ID original.</param>
    Task CrearConIdExplicitoAsync(Categoria categoria);

    /// <summary>
    ///     Re-sincroniza la secuencia de identidad tras altas con ID explícito.
    ///     Sin esto, la siguiente creación generaría un ID ya ocupado.
    /// </summary>
    Task ResincronizarSecuenciaAsync();

    /// <summary>Actualiza una categoría existente.</summary>
    /// <param name="categoria">Categoría con datos actualizados.</param>
    /// <returns>Categoría actualizada.</returns>
    Task<Categoria> UpdateAsync(Categoria categoria);

    /// <summary>Elimina una categoría (soft delete).</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>True si se eliminó.</returns>
    Task<bool> DeleteAsync(long id);

    /// <summary>Cuenta los productos que referencian la categoría (incluidos los borrados lógicos).</summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>Número de productos asociados.</returns>
    Task<int> CountProductosAsync(long categoriaId);

    /// <summary>Guarda cambios en la categoría.</summary>
    /// <param name="categoria">Categoría a guardar.</param>
    /// <returns>Categoría guardada.</returns>
    Task<Categoria> SaveAsync(Categoria categoria);
}
