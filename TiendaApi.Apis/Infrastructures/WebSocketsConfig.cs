using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.WebSockets.Pedidos;
using TiendaApi.Apis.WebSockets.Productos;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de WebSockets.
/// </summary>
public static class WebSocketsConfig
{
    /// <summary>
    /// Configura los handlers de WebSocket para notificaciones en tiempo real.
    /// </summary>
    public static IServiceCollection AddWebSockets(this IServiceCollection services)
    {
        Log.Information("🔌 Registrando handlers de WebSocket...");
        return services
            .AddSingleton<ProductoWebSocketHandler>()
            .AddSingleton<PedidoWebSocketHandler>();
    }
}
