using GraphQL.Types;
using TiendaApi.GraphQL.Types;

namespace TiendaApi.GraphQL;

/// <summary>
/// Schema principal de GraphQL para la tienda.
/// Registra el tipo de consulta raíz y los tipos de objeto.
/// </summary>
public class TiendaSchema : Schema
{
    /// <summary>
    /// Constructor del esquema de GraphQL.
    /// </summary>
    /// <param name="provider">Proveedor de servicios para la inyección de dependencias.</param>
    public TiendaSchema(IServiceProvider provider) : base(provider)
    {
        Query = provider.GetRequiredService<TiendaQuery>();
    }
}
