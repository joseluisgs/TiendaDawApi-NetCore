using GraphQL;
using GraphQL.Types;
using TiendaApi.Repositories;

namespace TiendaApi.GraphQL.Types;

/// <summary>
/// GraphQL Query root type
/// Exposes productos and productoById queries
/// </summary>
public class TiendaQuery : ObjectGraphType
{
    public TiendaQuery(IProductoRepository productoRepository, ICategoriaRepository categoriaRepository)
    {
        Name = "Query";

        Field<ListGraphType<ProductoType>>("productos")
            .ResolveAsync(async context =>
            {
                return await productoRepository.FindAllAsync();
            });

        Field<ProductoType>("productoById")
            .Argument<NonNullGraphType<IdGraphType>>("id")
            .ResolveAsync(async context =>
            {
                var id = context.GetArgument<long>("id");
                return await productoRepository.FindByIdAsync(id);
            });

        Field<ListGraphType<CategoriaType>>("categorias")
            .ResolveAsync(async context =>
            {
                return await categoriaRepository.FindAllAsync();
            });

        Field<CategoriaType>("categoriaById")
            .Argument<NonNullGraphType<IdGraphType>>("id")
            .ResolveAsync(async context =>
            {
                var id = context.GetArgument<long>("id");
                return await categoriaRepository.FindByIdAsync(id);
            });
    }
}
