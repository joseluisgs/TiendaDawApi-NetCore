using HotChocolate;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.GraphQL.Mutations;
using TiendaApi.Api.GraphQL.Queries;
using TiendaApi.Api.GraphQL.Subscriptions;
using TiendaApi.Api.GraphQL.Types;

namespace TiendaApi.Api.Infrastructures;

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
            .AddAuthorization()
            .AddQueryType<TiendaQuery>()
            .AddMutationType<ProductoMutation>()
            .AddSubscriptionType<ProductoSubscription>()
            .AddInMemorySubscriptions()
            .AddType<ProductoType>()
            .AddType<CategoriaType>()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = environment.IsDevelopment());
    }
}
