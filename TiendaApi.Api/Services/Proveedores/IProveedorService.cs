using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Proveedores;

/// <summary>
/// Contrato del servicio de proveedores.
/// Todas las operaciones son exclusivas del rol ADMIN.
/// </summary>
public interface IProveedorService
{
    /// <summary>Obtiene todos los proveedores.</summary>
    /// <returns>Lista de proveedores.</returns>
    Task<Result<IEnumerable<ProveedorDto>, DomainError>> FindAllAsync();

    /// <summary>Obtiene proveedores paginados con filtros.</summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Página de proveedores.</returns>
    Task<Result<PagedResult<ProveedorDto>, DomainError>> FindAllPagedAsync(ProveedorFilterDto filter);

    /// <summary>Busca un proveedor por su código.</summary>
    /// <param name="codigo">Código de 11 caracteres.</param>
    /// <returns>Proveedor encontrado.</returns>
    Task<Result<ProveedorDto, DomainError>> FindByIdAsync(string codigo);

    /// <summary>Crea un proveedor. Si el DTO no trae código, el servidor genera uno estilo YouTube.</summary>
    /// <param name="dto">Datos del proveedor.</param>
    /// <returns>Proveedor creado.</returns>
    Task<Result<ProveedorDto, DomainError>> CreateAsync(ProveedorRequestDto dto);

    /// <summary>Actualiza los datos editables de un proveedor. El código es la clave y no cambia.</summary>
    /// <param name="codigo">Código del proveedor a actualizar.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Proveedor actualizado.</returns>
    Task<Result<ProveedorDto, DomainError>> UpdateAsync(string codigo, ProveedorRequestDto dto);

    /// <summary>Elimina un proveedor (soft delete). Rechaza la operación si tiene productos asociados.</summary>
    /// <param name="codigo">Código del proveedor.</param>
    /// <returns>Vacío si tiene éxito.</returns>
    Task<UnitResult<DomainError>> DeleteAsync(string codigo);
}
