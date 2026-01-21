using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Realtime.Common;

namespace TiendaApi.Api.Realtime.Productos;

/// <summary>
/// Hub de SignalR para notificaciones de productos en tiempo real (público, sin auth).
/// </summary>
/// <example>
/// ws://localhost:5000/hubs/productos
/// const connection = new HubConnectionBuilder().withUrl("/hubs/productos").build();
/// connection.on("ProductoCreado", (producto) => console.log("Nuevo:", producto));
/// await connection.start();
/// // Respuesta: {"productoId":123,"nombre":"Nuevo","precio":99.99,"tipo":"PRODUCTO_CREADO","timestamp":"2025-01-18T10:30:00Z"}
/// </example>
[AllowAnonymous]
public class ProductosHub : Hub
{
    private readonly ILogger<ProductosHub> _logger;

    public ProductosHub(ILogger<ProductosHub> logger) => _logger = logger;

    /// <summary>Cliente conectado.</summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Cliente conectado: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>Cliente desconectado.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            _logger.LogWarning(exception, "Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        else
            _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Crea el payload de notificación.</summary>
    private object CreateNotificationPayload(string tipo, long productoId, ProductoDto? producto) => new
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
