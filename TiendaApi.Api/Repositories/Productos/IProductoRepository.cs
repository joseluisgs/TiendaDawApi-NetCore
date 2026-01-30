using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Productos;

/// <summary>
/// Define el contrato para el acceso a datos de productos.
/// Proporciona métodos para operaciones CRUD, consultas complejas, paginación y gestión de concurrencia.
/// </summary>
public interface IProductoRepository
{
    /// <summary>
    /// Recupera todos los productos de la base de datos, incluyendo su categoría.
    /// </summary>
    /// <returns>Una colección de entidades <see cref="Producto"/>.</returns>
    Task<IEnumerable<Producto>> FindAllAsync();

    /// <summary>
    /// Obtiene un flujo de consulta de productos sin seguimiento de cambios para optimizar lecturas.
    /// </summary>
    /// <returns>Un <see cref="IQueryable{Producto}"/>.</returns>
    IQueryable<Producto> FindAllAsNoTracking();

    /// <summary>
    /// Realiza una búsqueda paginada de productos aplicando múltiples filtros.
    /// </summary>
    /// <param name="filter">DTO con los parámetros de búsqueda y paginación.</param>
    /// <returns>Una tupla con el listado de productos y el conteo total para la paginación.</returns>
    Task<(IEnumerable<Producto> Items, int TotalCount)> FindAllPagedAsync(ProductoFilterDto filter);

    /// <summary>
    /// Busca un producto por su identificador único.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>La entidad <see cref="Producto"/> o null si no se encuentra.</returns>
    Task<Producto?> FindByIdAsync(long id);

    /// <summary>
    /// Recupera los productos asociados a una categoría específica.
    /// </summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>Lista de productos de dicha categoría.</returns>
    Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>
    /// Inserta un nuevo producto en la base de datos.
    /// </summary>
    /// <param name="producto">La entidad a guardar.</param>
    /// <returns>La entidad persistida con su ID generado.</returns>
    Task<Producto> SaveAsync(Producto producto);

    /// <summary>
    /// Actualiza los datos de un producto ya existente.
    /// </summary>
    /// <param name="producto">La entidad con los cambios.</param>
    /// <returns>La entidad actualizada.</returns>
    Task<Producto> UpdateAsync(Producto producto);

    /// <summary>
    /// Realiza una eliminación lógica del producto marcándolo como borrado.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    Task DeleteAsync(long id);

    /// <summary>
    /// Determina si un producto existe en el sistema.
    /// </summary>
    /// <param name="id">ID a comprobar.</param>
    /// <returns>True si existe.</returns>
    Task<bool> ExistsAsync(long id);

    /// <summary>
    /// Reduce el stock de un producto de forma atómica.
    /// Utiliza control de concurrencia optimista mediante RowVersion.
    /// </summary>
    /// <param name="productoId">ID del producto.</param>
    /// <param name="cantidad">Unidades a descontar.</param>
    /// <param name="expectedRowVersion">Versión de fila esperada para validar concurrencia.</param>
    /// <returns>True si la operación tuvo éxito.</returns>
    Task<bool> DecrementStockAsync(long productoId, int cantidad, byte[] expectedRowVersion);

    /// <summary>
    /// Inicia una transacción de base de datos con el nivel de aislamiento especificado.
    /// </summary>
    /// <param name="isolationLevel">Nivel de aislamiento (ej. Serializable).</param>
    /// <returns>Una instancia de transacción activa.</returns>
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel);

    /// <summary>
    /// Obtiene los productos que han sido creados en los últimos días.
    /// </summary>
    /// <param name="days">Número de días hacia atrás desde hoy.</param>
    /// <returns>Colección de productos recientes.</returns>
    Task<IEnumerable<Producto>> GetRecentlyCreatedAsync(int days);
}