using HotChocolate.Types;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para la entidad Categoria.
/// </summary>
public class CategoriaType : ObjectType<Categoria>
{
    protected override void Configure(IObjectTypeDescriptor<Categoria> descriptor)
    {
        descriptor.Name("Categoria");
        descriptor.Description("Entidad Categoria");

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>().Description("El ID de la categoría");
        descriptor.Field(c => c.Nombre).Type<NonNullType<StringType>>().Description("El nombre de la categoría");
        descriptor.Field(c => c.CreatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de creación");
        descriptor.Field(c => c.UpdatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de última actualización");
        descriptor.Field(c => c.IsDeleted).Type<NonNullType<BooleanType>>().Description("Si la categoría está eliminada");
    }
}
