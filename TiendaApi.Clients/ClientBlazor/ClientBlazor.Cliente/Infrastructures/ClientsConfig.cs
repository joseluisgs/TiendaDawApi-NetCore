using ClientBlazor.Cliente.Clients;
using ClientBlazor.Cliente.Configuration;
using ClientBlazor.Cliente.Infrastructures.Handlers;
using ClientBlazor.Cliente.State.Auth;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Refit;

namespace ClientBlazor.Cliente.Infrastructures;

/// <summary>
/// Contiene métodos de extensión para el registro de clientes de comunicación con la API.
/// </summary>
public static class ClientsConfig
{
    /// <summary>
    /// Configura y registra el cliente Refit para REST y el cliente para GraphQL en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <returns>La colección de servicios para configuración fluida.</returns>
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        // Registro de Refit con interceptor de token JWT
        services.AddRefitClient<ITiendaRestClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
            .AddHttpMessageHandler<AuthHeaderHandler>();

        // Registro de GraphQL con inyección dinámica de token en el payload del WebSocket
        services.AddScoped(sp =>
        {
            var authStore = sp.GetRequiredService<IAuthStore>();
            var options = new GraphQLHttpClientOptions
            {
                EndPoint = new Uri($"{AppConfig.ApiBaseUrl}/graphql"),
                WebSocketEndPoint = new Uri($"{AppConfig.ApiBaseUrl}/graphql".Replace("http", "ws")),
                ConfigureWebSocketConnectionInitPayload = (opts) =>
                {
                    var token = authStore.GetState().Token;
                    return new { Authorization = string.IsNullOrEmpty(token) ? "" : $"Bearer {token}" };
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