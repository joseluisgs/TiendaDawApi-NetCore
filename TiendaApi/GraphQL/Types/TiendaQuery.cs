using GraphQL;
using GraphQL.Types;
using TiendaApi.Repositories.Productos;
using TiendaApi.Repositories.Categorias;

namespace TiendaApi.GraphQL.Types;

/// <summary>
/// Tipo de consulta raíz de GraphQL para la tienda.
/// Expone las consultas de productos y categorías.
/// </summary>
public class TiendaQuery : ObjectGraphType
{
    /// <summary>
    /// Constructor del tipo de consulta.
    /// </summary>
    /// <param name="productoRepository">Repositorio de productos.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    public TiendaQuery(IProductoRepository productoRepository, ICategoriaRepository categoriaRepository)
    {
        Name = "Query";

        /// <summary>
        /// Consulta para obtener todos los productos.
        /// Returns: List<Producto>
        /// </summary>
        Field<ListGraphType<ProductoType>>("productos")
            .ResolveAsync(async context =>
            {
                return await productoRepository.FindAllAsync();
            });

        /// <summary>
        /// Consulta para obtener un producto por su ID.
        /// Returns: Producto
        /// </summary>
        Field<ProductoType>("productoById")
            .Argument<NonNullGraphType<IdGraphType>>("id")
            .ResolveAsync(async context =>
            {
                var id = context.GetArgument<long>("id");
                return await productoRepository.FindByIdAsync(id);
            });

        /// <summary>
        /// Consulta para obtener todas las categorías.
        /// Returns: List<Categoria>
        /// </summary>
        Field<ListGraphType<CategoriaType>>("categorias")
            .ResolveAsync(async context =>
            {
                return await categoriaRepository.FindAllAsync();
            });

        /// <summary>
        /// Consulta para obtener una categoría por su ID.
        /// Returns: Categoria
        /// </summary>
        Field<CategoriaType>("categoriaById")
            .Argument<NonNullGraphType<IdGraphType>>("id")
            .ResolveAsync(async context =>
            {
                var id = context.GetArgument<long>("id");
                return await categoriaRepository.FindByIdAsync(id);
            });
    }
}
