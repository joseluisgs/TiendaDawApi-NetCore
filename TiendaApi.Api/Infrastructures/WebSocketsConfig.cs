using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de WebSockets.
/// </summary>
public static class WebSocketsConfig
{
    /// <summary>
    /// Registra handlers de WebSocket para notificaciones en tiempo real.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddWebSockets(this IServiceCollection services)
    {
        Log.Information("Registrando handlers de WebSocket...");
        return services
            .AddSingleton<ProductosWebSocketHandler>()
            .AddSingleton<PedidosWebSocketHandler>();
    }
}
