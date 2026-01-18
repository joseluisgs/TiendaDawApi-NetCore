using Microsoft.AspNetCore.Builder;
using Serilog;
using TiendaApi.Apis.Realtime.Pedidos;
using TiendaApi.Apis.Realtime.Productos;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extension methods para WebSockets.
/// </summary>
public static class WebSocketExtensions
{
    /// <summary>
    /// Mapea los endpoints de WebSocket para productos y pedidos.
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <returns>Application builder.</returns>
    /// <remarks>
    /// <para><b>Endpoints:</b></para>
    /// <list type="bullet">
    ///   <item><code>/ws/productos</code> - Notificaciones públicas de productos.</item>
    ///   <item><code>/ws/pedidos?token=JWT</code> - Notificaciones privadas de pedidos.</item>
    /// </list>
    /// </remarks>
    public static IApplicationBuilder MapWebSocketEndpoints(this IApplicationBuilder app)
    {
        Log.Information("📡 Configurando endpoints WebSocket...");
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
