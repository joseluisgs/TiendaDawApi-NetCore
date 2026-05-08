using ClientBlazor.Cliente.Clients;
using ClientBlazor.Cliente.Configuration;
using ClientBlazor.Cliente.Infrastructures.Handlers;
using ClientBlazor.Cliente.State.Auth;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Refit;

namespace ClientBlazor.Cliente.Infrastructures;

/// <summary>
/// Contiene metodos de extension para el registro de clientes de comunicacion con la API.
/// </summary>
public static class ClientsConfig
{
    /// <summary>
    /// Configura y registra el cliente Refit para REST y el cliente para GraphQL en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Coleccion de servicios de la aplicacion.</param>
    /// <returns>La coleccion de servicios para configuracion fluida.</returns>
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        // Registro de Refit con interceptor de token JWT
        services.AddRefitClient<ITiendaRestClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
            .AddHttpMessageHandler<AuthHeaderHandler>();

        // Registro de GraphQL con suscripciones WebSocket
        services.AddScoped(sp =>
        {
            var authStore = sp.GetRequiredService<IAuthStore>();
            var options = new GraphQLHttpClientOptions
            {
                EndPoint = new Uri($"{AppConfig.ApiBaseUrl}/graphql"),
                WebSocketProtocol = "graphql-transport-ws",
                ConfigureWebSocketConnectionInitPayload = (opts) =>
                {
                    var token = authStore.GetState().Token;
                    return new { authorization = string.IsNullOrEmpty(token) ? "" : $"Bearer {token}" };
                }
            };
            
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("GraphQLClient");
            return new GraphQLHttpClient(options, new SystemTextJsonSerializer(), httpClient);
        });

        // Cliente HTTP nombrado para uso exclusivo de GraphQL (incluye interceptor de cabeceras)
        services.AddHttpClient("GraphQLClient", c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
            .AddHttpMessageHandler<AuthHeaderHandler>();

        return services;
    }
}