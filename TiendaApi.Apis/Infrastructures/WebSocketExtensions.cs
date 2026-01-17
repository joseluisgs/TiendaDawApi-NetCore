using Microsoft.AspNetCore.Builder;
using Serilog;
using TiendaApi.Apis.WebSockets.Pedidos;
using TiendaApi.Apis.WebSockets.Productos;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extension methods para WebSockets.
/// </summary>
public static class WebSocketExtensions
{
    /// <summary>
    /// Mapea los endpoints de WebSocket para productos y pedidos.
    /// </summary>
    public static IApplicationBuilder MapWebSocketEndpoints(this IApplicationBuilder app)
    {
        Log.Information("📡 Configurando endpoints WebSocket...");
        var webApp = (WebApplication)app;
        
        webApp.Map("/ws/v1/productos", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var ws = await context.WebSockets.AcceptWebSocketAsync();
                var handler = context.RequestServices.GetRequiredService<ProductoWebSocketHandler>();
                await handler.HandleConnectionAsync(context, ws);
            }
            else context.Response.StatusCode = 400;
        });

        webApp.Map("/ws/v1/pedidos", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var ws = await context.WebSockets.AcceptWebSocketAsync();
                var handler = context.RequestServices.GetRequiredService<PedidoWebSocketHandler>();
                await handler.HandleConnectionAsync(context, ws);
            }
            else context.Response.StatusCode = 400;
        });

        return app;
    }
}
