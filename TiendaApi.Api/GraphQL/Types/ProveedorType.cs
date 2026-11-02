using HotChocolate.Types;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para la entidad Proveedor.
/// </summary>
public class ProveedorType : ObjectType<Proveedor>
{
    /// <summary>
    /// Configura los campos y descripciones del tipo GraphQL de Proveedor.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<Proveedor> descriptor)
    {
        descriptor.Name("Proveedor");
        descriptor.Description("Entidad Proveedor");

        descriptor.Field(p => p.Codigo).Type<NonNullType<StringType>>().Description("El código del proveedor");
        descriptor.Field(p => p.Nombre).Type<NonNullType<StringType>>().Description("El nombre del proveedor");
        descriptor.Field(p => p.Descripcion).Type<StringType>().Description("La descripción del proveedor");
        descriptor.Field(p => p.Contacto).Type<StringType>().Description("El contacto del proveedor");
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de creación");
        descriptor.Field(p => p.UpdatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de última actualización");
        descriptor.Field(p => p.IsDeleted).Type<NonNullType<BooleanType>>().Description("Si el proveedor está eliminado");

        // Navegación EF: se excluye del esquema GraphQL. El listado de productos se pide
        // por sus queries (con paginación y filtros), no por navegación desde el proveedor.
        descriptor.Field(p => p.Productos).Ignore();
    }
}
