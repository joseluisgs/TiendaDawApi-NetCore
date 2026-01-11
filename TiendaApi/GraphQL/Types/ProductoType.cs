using GraphQL.Types;
using TiendaApi.Models;

namespace TiendaApi.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para la entidad Producto.
/// </summary>
public class ProductoType : ObjectGraphType<Producto>
{
    /// <summary>
    /// Constructor del tipo Producto.
    /// Define los campos disponibles para la consulta de productos.
    /// Returns: void
    /// </summary>
    public ProductoType()
    {
        Name = "Producto";
        Description = "Entidad Producto";

        Field(p => p.Id, type: typeof(IdGraphType)).Description("El ID del producto");
        Field(p => p.Nombre).Description("El nombre del producto");
        Field(p => p.Descripcion, nullable: true).Description("La descripción del producto");
        Field(p => p.Precio).Description("El precio del producto");
        Field(p => p.Stock).Description("Cantidad en stock");
        Field(p => p.Imagen, nullable: true).Description("URL de la imagen");
        Field(p => p.CategoriaId).Description("El ID de la categoría");
        Field(p => p.CreatedAt).Description("Fecha de creación");
        Field(p => p.UpdatedAt).Description("Fecha de última actualización");
        
        Field<CategoriaType>("categoria")
            .Resolve(context => context.Source.Categoria);
    }
}
