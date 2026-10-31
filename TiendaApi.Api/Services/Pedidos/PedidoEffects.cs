using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Services.Pedidos;

/// <summary>
/// 🎓 Efectos secundarios de PEDIDOS: WebSocket, SignalR y emails de estado
/// (cliente y administrador). Viven fuera de PedidosService para que ese
/// servicio se quede con la lógica de negocio (transacción, stock, estados) y
/// este sea el único sitio que cambia si cambia un canal: SRP + DIP (se
/// inyecta por IPedidoEffects). El reparto equivalente en el proyecto CQRS
/// son sus notifications + handlers.
/// </summary>
public class PedidoEffects(
    PedidosWebSocketHandler webSocketHandler,
    IHubContext<PedidosHub> pedidosHubContext,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<PedidoEffects> logger
) : IPedidoEffects
{
    #region ========== MÉTODOS PRIVADOS - WEBSOCKET ==========

    /// <summary>
    /// Notifica vía WebSocket la creación de un pedido.
    /// Envía notificación al usuario que creó el pedido y a todos los administradores.
    /// </summary>
    /// <param name="pedidoId">Identificador del pedido creado.</param>
    /// <param name="userId">ID del usuario que creó el pedido.</param>
    /// <param name="estado">Estado inicial del pedido.</param>
    public void NotificarWebSocketPedidoCreado(string pedidoId, long userId, string estado)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyUserAndAdminsAsync(userId, new PedidoNotificacion(
                    PedidoNotificationType.CREADO,
                    pedidoId,
                    userId,
                    estado,
                    null
                ));
                logger.LogDebug("Notificación WebSocket enviada para pedido: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    /// <summary>
    /// Notifica vía WebSocket la actualización del estado de un pedido.
    /// Envía notificación al usuario afectado y a todos los administradores.
    /// </summary>
    /// <param name="pedidoId">Identificador del pedido.</param>
    /// <param name="userId">ID del usuario que realizó el pedido.</param>
    /// <param name="estado">Nuevo estado del pedido.</param>
    /// <param name="pedido">Datos actualizados del pedido.</param>
    public void NotificarWebSocketPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyUserAndAdminsAsync(userId, new PedidoNotificacion(
                    PedidoNotificationType.ESTADO_ACTUALIZADO,
                    pedidoId,
                    userId,
                    estado,
                    pedido
                ));
                logger.LogDebug("Notificación WebSocket enviada para pedido: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    #endregion

    #region ========== MÉTODOS PRIVADOS - SIGNALR ==========

    /// <summary>
    /// Notifica via SignalR la creacion de un pedido.
    /// </summary>
    public void NotificarSignalRPedidoCreado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    pedidoId,
                    userId,
                    estado,
                    tipo = "PEDIDO_CREADO",
                    total = pedido.Total,
                    itemsCount = pedido.Items?.Count ?? 0,
                    timestamp = DateTime.UtcNow
                };
                await pedidosHubContext.Clients.All.SendAsync("PedidoCreado", payload);
                logger.LogDebug("Notificación SignalR enviada para pedido creado: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    /// <summary>
    /// Notifica via SignalR la actualizacion de un pedido.
    /// </summary>
    public void NotificarSignalRPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    pedidoId,
                    userId,
                    estado,
                    tipo = "PEDIDO_ACTUALIZADO",
                    total = pedido.Total,
                    timestamp = DateTime.UtcNow
                };
                await pedidosHubContext.Clients.All.SendAsync("PedidoActualizado", payload);
                logger.LogDebug("Notificación SignalR enviada para pedido actualizado: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    /// <summary>
    /// Notifica via SignalR la eliminacion de un pedido.
    /// </summary>
    public void NotificarSignalRPedidoEliminado(string pedidoId, long userId, string estado)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    pedidoId,
                    userId,
                    estado,
                    tipo = "PEDIDO_ELIMINADO",
                    timestamp = DateTime.UtcNow
                };
                await pedidosHubContext.Clients.All.SendAsync("PedidoEliminado", payload);
                logger.LogDebug("Notificación SignalR enviada para pedido eliminado: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    #endregion

    #region ========== MÉTODOS PRIVADOS - EMAIL ==========

    /// <summary>
    /// Encola el email de bienvenida al cliente tras crear un pedido.
    /// </summary>
    public void EnviarEmailPedidoCreado(string pedidoId, decimal total, int itemCount, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoCreado(pedidoId, total, itemCount, userId);
                var body = EmailTemplates.CreateBase("Nuevo Pedido Recibido", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"🛒 Nuevo Pedido #{pedidoId}",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras crear pedido");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras crear pedido");
            }
        });
    }

    /// <summary>
    /// Encola el email de cambio de estado (cliente).
    /// </summary>
    public void EnviarEmailPedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoEstadoActualizado(pedidoId, estadoAnterior, nuevoEstado, total, userId);
                var body = EmailTemplates.CreateBase("Cambio de Estado de Pedido", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"📦 Pedido #{pedidoId} - {nuevoEstado}",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras cambio de estado del pedido");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras cambio de estado");
            }
        });
    }

    /// <summary>
    /// Encola el email de aviso al administrador al actualizar un pedido.
    /// </summary>
    public void EnviarEmailPedidoActualizadoAdmin(string pedidoId, string estado, decimal total, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoActualizadoAdmin(pedidoId, estado, total, userId);
                var body = EmailTemplates.CreateBase("Pedido Actualizado por Administrador", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"✏️ Pedido #{pedidoId} Actualizado",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras actualización de pedido por admin");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras actualización por admin");
            }
        });
    }

    /// <summary>
    /// Encola el email de aviso al administrador al eliminar un pedido.
    /// </summary>
    public void EnviarEmailPedidoEliminadoAdmin(string pedidoId, decimal total, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoEliminadoAdmin(pedidoId, total, userId);
                var body = EmailTemplates.CreateBase("Pedido Eliminado", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"🗑️ Pedido #{pedidoId} Eliminado",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras eliminación de pedido por admin");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras eliminación por admin");
            }
        });
    }

    #endregion

}
