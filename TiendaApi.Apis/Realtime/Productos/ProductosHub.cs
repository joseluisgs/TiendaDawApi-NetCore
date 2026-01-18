using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Realtime.Common;

namespace TiendaApi.Apis.Realtime.Productos;

/// <summary>
/// Hub de SignalR para notificaciones en tiempo real de productos.
/// Hace broadcast a todos los clientes conectados (público, sin auth).
/// </summary>
/// <remarks>
/// <para><b>Características:</b></para>
/// <list type="bullet">
///   <item><description>Notificaciones de broadcast a TODOS los clientes conectados.</description></item>
///   <item><description>No requiere autenticación (público).</description></item>
///   <item><description>Ideal para dashboards públicos y catálogos en tiempo real.</description></item>
/// </list>
/// 
/// <para><b>Endpoint:</b></para>
/// <code>ws://localhost:5000/hubs/productos</code>
/// 
/// <para><b>Conexión desde cliente JavaScript:</b></para>
/// <code>
/// const connection = new HubConnectionBuilder()
///     .withUrl("/hubs/productos")
///     .build();
///
/// connection.on("ProductoCreado", (producto) => {
///     console.log("Nuevo producto:", producto);
/// });
///
/// await connection.start();
/// </code>
/// 
/// <para><b>Eventos recibidos:</b></para>
/// <list type="table">
///   <item>
///     <term>ProductoCreado</term>
///     <description>Se creó un nuevo producto. Envía datos completos.</description>
///   </item>
///   <item>
///     <term>ProductoActualizado</term>
///     <description>Se actualizó un producto. Envía datos actualizados.</description>
///   </item>
///   <item>
///     <term>ProductoEliminado</term>
///     <description>Se eliminó un producto. Envía solo el ID.</description>
///   </item>
/// </list>
/// 
/// <para><b>Ejemplo de respuesta:</b></para>
/// <code>
/// {
///   "productoId": 123,
///   "nombre": "Nuevo Producto",
///   "precio": 99.99,
///   "stock": 50,
///   "tipo": "PRODUCTO_CREADO",
///   "timestamp": "2025-01-18T10:30:00Z"
/// }
/// </code>
/// </remarks>
[AllowAnonymous]
public class ProductosHub : Hub
{
    private readonly ILogger<ProductosHub> _logger;

    public ProductosHub(ILogger<ProductosHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Se ejecuta cuando un cliente se conecta.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Cliente SignalR conectado a ProductosHub: {ConnectionId}", connectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Se ejecuta cuando un cliente se desconecta.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        if (exception != null)
        {
            _logger.LogWarning(exception, "Cliente SignalR desconectado con error: {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogInformation("Cliente SignalR desconectado: {ConnectionId}", connectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Crea el payload de notificación para enviar a clientes.
    /// </summary>
    private object CreateNotificationPayload(string tipo, long productoId, ProductoDto? producto)
    {
        return new
        {
            productoId,
            nombre = producto?.Nombre,
            descripcion = producto?.Descripcion,
            precio = producto?.Precio,
            stock = producto?.Stock,
            imagen = producto?.Imagen,
            categoriaId = producto?.CategoriaId,
            categoriaNombre = producto?.CategoriaNombre,
            tipo,
            timestamp = DateTime.UtcNow
        };
    }
}
