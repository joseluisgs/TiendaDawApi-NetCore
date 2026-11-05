using HotChocolate.Types;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para <see cref="ProveedorResumenDto"/>: el resumen de
/// proveedor que sirve REST dentro de un producto (código y nombre).
///
/// 🎓 Opción "un solo contrato": sobre el DTO, con <c>BindFieldsExplicitly</c>.
/// Antes este type apuntaba a la entidad Proveedor completa (7 campos) y
/// quedaba inalcanzable en el esquema: nadie podía consultarlo nunca.
/// </summary>
public class ProveedorType : ObjectType<ProveedorResumenDto>
{
    /// <summary>
    /// Configura los campos y descripciones del tipo GraphQL de ProveedorResumenDto.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<ProveedorResumenDto> descriptor)
    {
        descriptor.Name("ProveedorResumenDto");
        descriptor.Description("Resumen de proveedor dentro de un producto (mismo contrato que REST).");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Codigo).Description("El código del proveedor");
        descriptor.Field(p => p.Nombre).Description("El nombre del proveedor");
        descriptor.Field(p => p.Descripcion).Description("La descripción del proveedor (null si no tiene)");
    }
}
