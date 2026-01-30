using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Productos;

/// <summary>
/// Contrato del repositorio de productos.
/// </summary>
public interface IProductoRepository
{
    /// <summary>Obtiene todos los productos ordenados por nombre.</summary>
    /// <returns>Colección de productos.</returns>
    Task<IEnumerable<Producto>> FindAllAsync();

    /// <summary>Obtiene productos como IQueryable para GraphQL.</summary>
    /// <returns>IQueryable de productos.</returns>
    IQueryable<Producto> FindAllAsNoTracking();

    /// <summary>Obtiene productos paginados con filtros.</summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Tupla con items y total.</returns>
    Task<(IEnumerable<Producto> Items, int TotalCount)> FindAllPagedAsync(ProductoFilterDto filter);

    /// <summary>Busca un producto por ID.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>Producto o null.</returns>
    Task<Producto?> FindByIdAsync(long id);

    /// <summary>Obtiene productos por categoría.</summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>Colección de productos.</returns>
    Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>Guarda o actualiza un producto.</summary>
    /// <param name="producto">Producto a guardar.</param>
    /// <returns>Producto guardado.</returns>
    Task<Producto> SaveAsync(Producto producto);

    /// <summary>Actualiza un producto existente.</summary>
    /// <param name="producto">Producto con datos actualizados.</param>
    /// <returns>Producto actualizado.</returns>
    Task<Producto> UpdateAsync(Producto producto);

    /// <summary>Elimina un producto (soft delete).</summary>
    /// <param name="id">ID del producto.</param>
    Task DeleteAsync(long id);

    /// <summary>Verifica si existe un producto por ID.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>True si existe.</returns>
    Task<bool> ExistsAsync(long id);

    /// <summary>Decrementa stock con control de concurrencia.</summary>
    /// <param name="productoId">ID del producto.</param>
    /// <param name="cantidad">Cantidad a restar.</param>
    /// <param name="expectedRowVersion">Versión original del registro.</param>
    /// <returns>True si se decrementó exitosamente.</returns>
    Task<bool> DecrementStockAsync(long productoId, int cantidad, byte[] expectedRowVersion);

    /// <summary>Inicia una transacción.</summary>
    /// <param name="isolationLevel">Nivel de aislamiento.</param>
    /// <returns>Transacción iniciada.</returns>
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel);

    /// <summary>Obtiene productos creados recientemente.</summary>
    /// <param name="days">Días hacia atrás.</param>
    /// <returns>Colección de productos.</returns>
    Task<IEnumerable<Producto>> GetRecentlyCreatedAsync(int days);
}
