using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Mapper para convertir entre Proveedor y sus DTOs.
/// </summary>
public static class ProveedorMapper {
    /// <summary>Convierte Proveedor a ProveedorDto.</summary>
    public static ProveedorDto ToDto(this Proveedor proveedor) =>
        new(proveedor.Codigo, proveedor.Nombre, proveedor.Descripcion, proveedor.Contacto,
            proveedor.CreatedAt, proveedor.UpdatedAt);

    /// <summary>Convierte lista de Proveedores a lista de ProveedorDto.</summary>
    public static IEnumerable<ProveedorDto> ToDtoList(this IEnumerable<Proveedor> proveedores) =>
        proveedores.Select(p => p.ToDto());

    /// <summary>
    /// Convierte ProveedorRequestDto a Proveedor.
    /// El código solo se copia si viene en la petición; si queda a null, Entity Framework
    /// genera el identificador con el generador de IDs estilo YouTube al registrar la entidad.
    /// </summary>
    public static Proveedor ToEntity(this ProveedorRequestDto dto) {
        var proveedor = new Proveedor {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Contacto = dto.Contacto,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(dto.Codigo))
            proveedor.Codigo = dto.Codigo;

        return proveedor;
    }

    /// <summary>Actualiza los campos editables de un Proveedor desde su DTO.</summary>
    public static void UpdateEntity(this ProveedorRequestDto dto, Proveedor proveedor) {
        proveedor.Nombre = dto.Nombre;
        proveedor.Descripcion = dto.Descripcion;
        proveedor.Contacto = dto.Contacto;
    }
}
