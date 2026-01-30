using ClientBlazor.Cliente.Services.GraphQL;
using ClientBlazor.Cliente.Services.Rest;
using ClientBlazor.Cliente.Services.SignalR;
using ClientBlazor.Cliente.Services.Websocket;
using ClientBlazor.Cliente.Services.Storage;

namespace ClientBlazor.Cliente.Infrastructures;

public static class ServicesConfig
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ILocalStorageService, LocalStorageService>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IRestService, ClientBlazor.Cliente.Services.Rest.RestService>();
        services.AddTransient<IGraphQLService, GraphQLService>();
        services.AddSingleton<IWebSocketService, WebSocketService>();
        services.AddSingleton<ISignalRService, SignalRService>();
        
        return services;
    }
}