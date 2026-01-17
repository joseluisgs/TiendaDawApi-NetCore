using HotChocolate;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.GraphQL.Mutations;
using TiendaApi.Apis.GraphQL.Queries;
using TiendaApi.Apis.GraphQL.Subscriptions;
using TiendaApi.Apis.GraphQL.Types;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de GraphQL con HotChocolate.
/// </summary>
public static class GraphQLConfig
{
    /// <summary>
    /// Configura GraphQL con queries de productos y categorías.
    /// </summary>
    public static IRequestExecutorBuilder AddGraphQL(this IServiceCollection services, IWebHostEnvironment environment)
    {
        Log.Information("🔍 Configurando GraphQL con HotChocolate...");
        return services
            .AddGraphQLServer()
            .AddQueryType<TiendaQuery>()
            .AddMutationType<ProductoMutation>()
            .AddSubscriptionType<ProductoSubscription>()
            .AddInMemorySubscriptions()
            .AddType<ProductoType>()
            .AddType<CategoriaType>()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = environment.IsDevelopment());
    }
}
