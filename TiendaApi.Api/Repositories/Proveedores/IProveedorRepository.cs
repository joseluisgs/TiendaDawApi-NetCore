using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Proveedores;

/// <summary>
/// Contrato del repositorio de proveedores.
/// La clave es un identificador de 11 caracteres generado por la aplicación.
/// </summary>
public interface IProveedorRepository {
    /// <summary>Obtiene todos los proveedores ordenados por nombre.</summary>
    /// <returns>Colección de proveedores.</returns>
    Task<IEnumerable<Proveedor>> FindAllAsync();

    /// <summary>Obtiene proveedores como IQueryable para consultas composables.</summary>
    /// <returns>IQueryable de proveedores.</returns>
    IQueryable<Proveedor> FindAllAsNoTracking();

    /// <summary>Obtiene proveedores paginados con filtros.</summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Tupla con items y total de registros.</returns>
    Task<(IEnumerable<Proveedor> Items, int TotalCount)> FindAllPagedAsync(ProveedorFilterDto filter);

    /// <summary>Busca un proveedor por su código.</summary>
    /// <param name="codigo">Código de 11 caracteres del proveedor.</param>
    /// <returns>Proveedor encontrado o null.</returns>
    Task<Proveedor?> FindByIdAsync(string codigo);

    /// <summary>Verifica si existe un proveedor con ese código.</summary>
    /// <param name="codigo">Código a comprobar.</param>
    /// <returns>True si existe.</returns>
    Task<bool> ExistsByIdAsync(string codigo);

    /// <summary>Verifica si existe un proveedor con ese nombre.</summary>
    /// <param name="nombre">Nombre del proveedor.</param>
    /// <param name="excludeCodigo">Código a excluir (para actualizaciones).</param>
    /// <returns>True si existe.</returns>
    Task<bool> ExistsByNombreAsync(string nombre, string? excludeCodigo = null);

    /// <summary>Cuenta los productos activos asociados a un proveedor.</summary>
    /// <param name="codigo">Código del proveedor.</param>
    /// <returns>Número de productos que apuntan al proveedor.</returns>
    Task<int> CountProductosAsync(string codigo);

    /// <summary>Crea un nuevo proveedor (el código lo genera el mapeador o llega en el DTO).</summary>
    /// <param name="proveedor">Proveedor a crear.</param>
    /// <returns>Proveedor creado, con el código definitivo.</returns>
    Task<Proveedor> CreateAsync(Proveedor proveedor);

    /// <summary>Actualiza un proveedor existente (el código es la clave y no cambia).</summary>
    /// <param name="proveedor">Proveedor con datos actualizados.</param>
    /// <returns>Proveedor actualizado.</returns>
    Task<Proveedor> UpdateAsync(Proveedor proveedor);

    /// <summary>Elimina un proveedor (soft delete).</summary>
    /// <param name="codigo">Código del proveedor.</param>
    /// <returns>True si se eliminó.</returns>
    Task<bool> DeleteAsync(string codigo);
}
