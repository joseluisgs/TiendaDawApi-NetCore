using GraphQL.Types;
using TiendaApi.Models.Entities;

namespace TiendaApi.GraphQL.Types;

/// <summary>
/// GraphQL type for Producto entity
/// </summary>
public class ProductoType : ObjectGraphType<Producto>
{
    public ProductoType()
    {
        Name = "Producto";
        Description = "Producto entity";

        Field(p => p.Id, type: typeof(IdGraphType)).Description("The ID of the producto");
        Field(p => p.Nombre).Description("The name of the producto");
        Field(p => p.Descripcion, nullable: true).Description("The description of the producto");
        Field(p => p.Precio).Description("The price of the producto");
        Field(p => p.Stock).Description("Stock quantity");
        Field(p => p.Imagen, nullable: true).Description("Image URL");
        Field(p => p.CategoriaId).Description("The ID of the categoria");
        Field(p => p.CreatedAt).Description("Creation timestamp");
        Field(p => p.UpdatedAt).Description("Last update timestamp");
        
        Field<CategoriaType>("categoria")
            .Resolve(context => context.Source.Categoria);
    }
}
