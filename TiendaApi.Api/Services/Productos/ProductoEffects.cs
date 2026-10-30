using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Events;
using TiendaApi.Api.GraphQL.Publishers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Realtime.Productos;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// 🎓 Efectos secundarios de PRODUCTOS: WebSocket, SignalR, email de aviso al
/// administrador y eventos de suscripción GraphQL. Viven fuera de
/// ProductoService para que ese servicio se quede con lo que es (leer,
/// escribir, validar, cachear) y este sea el único sitio que cambia si cambia
/// un canal de notificación: SRP + DIP (se inyecta por IProductoEffects). El
/// reparto equivalente en el proyecto CQRS son sus notifications + handlers.
/// </summary>
public class ProductoEffects(
    ProductosWebSocketHandler webSocketHandler,
    IHubContext<ProductosHub> productosHubContext,
    IEmailService emailService,
    IConfiguration configuration,
    IEventPublisher eventPublisher,
    ILogger<ProductoEffects> logger
) : IProductoEffects
{
    #region Métodos Privados - WebSocket Nativo

    /// <summary>
    /// Notifica vía WebSocket la creación de un producto.
    /// </summary>
    public void NotificarWebSocketProductoCreado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new ProductoNotificacion(
                    ProductoNotificationType.CREATED,
                    producto.Id,
                    producto
                ));
                logger.LogInformation("📡 [WEBSOCKET] Notificación enviada: Producto creado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket al crear producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía WebSocket la actualización de un producto.
    /// </summary>
    public void NotificarWebSocketProductoActualizado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new ProductoNotificacion(
                    ProductoNotificationType.UPDATED,
                    producto.Id,
                    producto
                ));
                logger.LogInformation("📡 [WEBSOCKET] Notificación enviada: Producto actualizado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket al actualizar producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía WebSocket la eliminación de un producto.
    /// </summary>
    public void NotificarWebSocketProductoEliminado(long productoId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new ProductoNotificacion(
                    ProductoNotificationType.DELETED,
                    productoId,
                    null
                ));
                logger.LogInformation("📡 [WEBSOCKET] Notificación enviada: Producto eliminado ID={ProductoId}", productoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket al eliminar producto: {ProductoId}", productoId);
            }
        });
    }

    #endregion

    #region Métodos Privados - SignalR

    /// <summary>
    /// Notifica vía SignalR la creación de un producto.
    /// </summary>
    public void NotificarSignalRProductoCreado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    productoId = producto.Id,
                    nombre = producto.Nombre,
                    descripcion = producto.Descripcion,
                    precio = producto.Precio,
                    stock = producto.Stock,
                    categoriaId = producto.CategoriaId,
                    categoriaNombre = producto.CategoriaNombre,
                    tipo = "PRODUCTO_CREADO",
                    timestamp = DateTime.UtcNow
                };
                await productosHubContext.Clients.All.SendAsync("ProductoCreado", payload);
                logger.LogInformation("📟 [SIGNALR] Notificación enviada: Producto creado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR al crear producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía SignalR la actualización de un producto.
    /// </summary>
    public void NotificarSignalRProductoActualizado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    productoId = producto.Id,
                    nombre = producto.Nombre,
                    descripcion = producto.Descripcion,
                    precio = producto.Precio,
                    stock = producto.Stock,
                    categoriaId = producto.CategoriaId,
                    categoriaNombre = producto.CategoriaNombre,
                    tipo = "PRODUCTO_ACTUALIZADO",
                    timestamp = DateTime.UtcNow
                };
                await productosHubContext.Clients.All.SendAsync("ProductoActualizado", payload);
                logger.LogInformation("📟 [SIGNALR] Notificación enviada: Producto actualizado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR al actualizar producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía SignalR la eliminación de un producto.
    /// </summary>
    public void NotificarSignalRProductoEliminado(long productoId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    productoId,
                    tipo = "PRODUCTO_ELIMINADO",
                    timestamp = DateTime.UtcNow
                };
                await productosHubContext.Clients.All.SendAsync("ProductoEliminado", payload);
                logger.LogInformation("📟 [SIGNALR] Notificación enviada: Producto eliminado ID={ProductoId}", productoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR al eliminar producto: {ProductoId}", productoId);
            }
        });
    }

    #endregion

    #region Métodos Privados - Email

    /// <summary>
    /// Envía email de notificación cuando se crea un producto.
    /// </summary>
    public void EnviarEmailProductoCreado(Producto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.ProductoCreado(producto.Nombre, producto.Precio, producto.Stock, producto.Id);
                var body = EmailTemplates.CreateBase("Nuevo Producto Creado", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = "🆕 Nuevo Producto en Tienda DAW",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras crear producto");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras crear producto");
            }
        });
    }

    #endregion

    #region Métodos Privados - GraphQL Subscriptions

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando se crea un producto.
    /// </summary>
    public void EventoSuscripcionProductoCreado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onProductoCreado", new ProductoCreadoEvent
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Stock = producto.Stock,
                    CreatedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Producto creado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription al crear producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando se actualiza un producto.
    /// </summary>
    public void EventoSuscripcionProductoActualizado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onProductoActualizado", new ProductoActualizadoEvent
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Stock = producto.Stock,
                    UpdatedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Producto actualizado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription al actualizar producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando se elimina un producto.
    /// </summary>
    public void EventoSuscripcionProductoEliminado(long productoId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onProductoEliminado", new ProductoEliminadoEvent
                {
                    ProductoId = productoId,
                    DeletedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Producto eliminado ID={ProductoId}", productoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription al eliminar producto: {ProductoId}", productoId);
            }
        });
    }

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando el stock está bajo.
    /// </summary>
    public void EventoSuscripcionStockBajo(ProductoDto producto, int umbralStock)
    {
        if (producto.Stock > umbralStock) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onStockBajo", new ProductoStockBajoEvent
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    StockActual = producto.Stock,
                    UmbralStock = umbralStock,
                    DetectedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Stock bajo ID={ProductoId}, Stock={Stock}", producto.Id, producto.Stock);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription de stock bajo: {ProductoId}", producto.Id);
            }
        });
    }

    #endregion

}
