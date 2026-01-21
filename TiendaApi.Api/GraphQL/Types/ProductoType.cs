using HotChocolate.Types;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para la entidad Producto.
/// </summary>
public class ProductoType : ObjectType<Producto>
{
    protected override void Configure(IObjectTypeDescriptor<Producto> descriptor)
    {
        descriptor.Name("Producto");
        descriptor.Description("Entidad Producto");

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>().Description("El ID del producto");
        descriptor.Field(p => p.Nombre).Type<NonNullType<StringType>>().Description("El nombre del producto");
        descriptor.Field(p => p.Descripcion).Type<StringType>().Description("La descripción del producto");
        descriptor.Field(p => p.Precio).Type<NonNullType<DecimalType>>().Description("El precio del producto");
        descriptor.Field(p => p.Stock).Type<NonNullType<IntType>>().Description("Cantidad en stock");
        descriptor.Field(p => p.Imagen).Type<StringType>().Description("URL de la imagen");
        descriptor.Field(p => p.CategoriaId).Type<NonNullType<IntType>>().Description("El ID de la categoría");
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de creación");
        descriptor.Field(p => p.UpdatedAt).Type<NonNullType<DateTimeType>>().Description("Fecha de última actualización");
        descriptor.Field(p => p.IsDeleted).Type<NonNullType<BooleanType>>().Description("Si el producto está eliminado");

        descriptor.Field(p => p.Categoria)
            .Type<CategoriaType>()
            .Description("La categoría del producto");
    }
}
