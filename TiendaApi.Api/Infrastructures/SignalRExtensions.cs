using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de SignalR.
/// </summary>
public static class SignalRExtensions
{
    /// <summary>
    /// Configura Hubs de SignalR (ProductosHub, PedidosHub).
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddRealtimeSignalR(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddHubOptions<ProductosHub>(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 4;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            })
            .AddHubOptions<PedidosHub>(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 4;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

        return services;
    }

    /// <summary>
    /// Mapea endpoints de SignalR (/hubs/productos, /hubs/pedidos).
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <returns>Application builder.</returns>
    public static IApplicationBuilder MapSignalRHubs(this IApplicationBuilder app)
    {
        var webApp = (WebApplication)app;
        
        webApp.MapHub<ProductosHub>("/hubs/productos");
        webApp.MapHub<PedidosHub>("/hubs/pedidos");

        return app;
    }
}
