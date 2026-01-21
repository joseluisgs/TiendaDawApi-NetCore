using Microsoft.AspNetCore.Builder;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de WebSockets.
/// </summary>
public static class WebSocketExtensions
{
    /// <summary>
    /// Mapea endpoints WebSocket (/ws/productos, /ws/pedidos?token=JWT).
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <returns>Application builder.</returns>
    public static IApplicationBuilder MapWebSocketEndpoints(this IApplicationBuilder app)
    {
        Log.Information("Configurando endpoints WebSocket...");
        var webApp = (WebApplication)app;
        
        webApp.Map("/ws/productos", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var ws = await context.WebSockets.AcceptWebSocketAsync();
                var handler = context.RequestServices.GetRequiredService<ProductosWebSocketHandler>();
                await handler.HandleConnectionAsync(context, ws);
            }
            else context.Response.StatusCode = 400;
        });

        webApp.Map("/ws/pedidos", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var ws = await context.WebSockets.AcceptWebSocketAsync();
                var handler = context.RequestServices.GetRequiredService<PedidosWebSocketHandler>();
                await handler.HandleConnectionAsync(context, ws);
            }
            else context.Response.StatusCode = 400;
        });

        return app;
    }
}
