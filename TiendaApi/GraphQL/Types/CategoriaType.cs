using GraphQL.Types;
using TiendaApi.Models.Entities;

namespace TiendaApi.GraphQL.Types;

/// <summary>
/// GraphQL type for Categoria entity
/// </summary>
public class CategoriaType : ObjectGraphType<Categoria>
{
    public CategoriaType()
    {
        Name = "Categoria";
        Description = "Categoria entity";

        Field(c => c.Id, type: typeof(IdGraphType)).Description("The ID of the categoria");
        Field(c => c.Nombre).Description("The name of the categoria");
        Field(c => c.CreatedAt).Description("Creation timestamp");
        Field(c => c.UpdatedAt).Description("Last update timestamp");
    }
}
