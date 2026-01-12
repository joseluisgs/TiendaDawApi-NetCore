using GraphQL.Types;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para la entidad Categoria.
/// </summary>
public class CategoriaType : ObjectGraphType<Categoria>
{
    /// <summary>
    /// Constructor del tipo Categoria.
    /// Define los campos disponibles para la consulta de categorías.
    /// Returns: void
    /// </summary>
    public CategoriaType()
    {
        Name = "Categoria";
        Description = "Entidad Categoria";

        Field(c => c.Id, type: typeof(IdGraphType)).Description("El ID de la categoría");
        Field(c => c.Nombre).Description("El nombre de la categoría");
        Field(c => c.CreatedAt).Description("Fecha de creación");
        Field(c => c.UpdatedAt).Description("Fecha de última actualización");
    }
}
