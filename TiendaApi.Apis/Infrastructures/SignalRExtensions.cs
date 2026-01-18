using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Apis.Realtime.Pedidos;
using TiendaApi.Apis.Realtime.Productos;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones para configurar SignalR en la aplicación.
/// </summary>
public static class SignalRExtensions
{
    /// <summary>
    /// Configura los Hubs de SignalR para tiempo real.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La colección de servicios.</returns>
    /// <remarks>
    /// <para><b>Configuración de Hubs:</b></para>
    /// <code>
    /// builder.Services.AddRealtimeSignalR();
    /// </code>
    /// 
    /// <para><b>Hubs registrados:</b></para>
    /// <list type="bullet">
    ///   <item><description>ProductosHub: Notificaciones de productos.</description></item>
    ///   <item><description>PedidosHub: Notificaciones de pedidos con auth.</description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddRealtimeSignalR(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddHubOptions<ProductosHub>(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 4; // 4KB
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
    /// Mapea los endpoints de SignalR.
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <returns>Application builder.</returns>
    /// <remarks>
    /// <para><b>Endpoints mapeados:</b></para>
    /// <code>
    /// app.MapSignalRHubs();
    /// // Genera:
    /// // - /hubs/productos
    /// // - /hubs/pedidos
    /// </code>
    /// </remarks>
    public static IApplicationBuilder MapSignalRHubs(this IApplicationBuilder app)
    {
        var webApp = (WebApplication)app;
        
        webApp.MapHub<ProductosHub>("/hubs/productos");
        webApp.MapHub<PedidosHub>("/hubs/pedidos");

        return app;
    }
}
