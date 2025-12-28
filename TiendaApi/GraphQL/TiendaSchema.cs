using GraphQL.Types;
using TiendaApi.GraphQL.Types;

namespace TiendaApi.GraphQL;

/// <summary>
/// GraphQL Schema for TiendaApi
/// Registers root query type and object types
/// </summary>
public class TiendaSchema : Schema
{
    public TiendaSchema(IServiceProvider provider) : base(provider)
    {
        Query = provider.GetRequiredService<TiendaQuery>();
    }
}
