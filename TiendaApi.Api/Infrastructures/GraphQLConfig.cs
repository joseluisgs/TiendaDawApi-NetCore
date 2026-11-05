using HotChocolate;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.GraphQL.Mutations;
using TiendaApi.Api.GraphQL.Publishers;
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

        services.AddGraphQLPubSub();

        var builder = services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<TiendaQuery>()
            .AddMutationType<ProductoMutation>()
            .AddSubscriptionType<ProductoSubscription>()
            .AddInMemorySubscriptions()
            .AddType<ProductoType>()
            .AddType<CategoriaType>()
            .AddType<ProveedorType>()
            .AddType<PedidoType>()
            .AddType<DestinatarioType>()
            .AddType<DireccionType>()
            .AddType<PedidoItemType>()
            // 🎓 Opción "un solo contrato": los types se construyen sobre los
            // DTOs y RemoveUnreachableTypes elimina del esquema cualquier tipo
            // registrado al que ninguna operación pueda llegar. Así la
            // introspección nunca vuelve a prometer campos que no se pueden
            // consultar (antes: los types entidad Producto/Categoria/Proveedor).
            .ModifyOptions(options => options.RemoveUnreachableTypes = true)
            // 🛡️ Límite de profundidad de ejecución: evita queries anidadas
            // infinitas que consuman memoria/CPU. 10 niveles es más que
            // suficiente para cualquier consulta legítima de este proyecto.
            // skipIntrospectionFields: true → no cuenta la introspección.
            .AddMaxExecutionDepthRule(
                maxAllowedExecutionDepth: 10,
                skipIntrospectionFields: true)
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = environment.IsDevelopment();
            });

        // 🛡️ Introspección: solo en desarrollo. En producción permite
        // descubrir el esquema completo del API (query/mutation names,
        // tipos, argumentos), lo que facilita ataques dirigidos.
        if (!environment.IsDevelopment())
        {
            builder.DisableIntrospection();
        }

        return builder;
    }

    /// <summary>
    /// Configura los endpoints de GraphQL incluyendo WebSocket para suscripciones.
    /// </summary>
    public static void MapGraphQLEndpoints(this WebApplication app)
    {
        app.MapGraphQL();
        app.MapGraphQLWebSocket();
    }
}
