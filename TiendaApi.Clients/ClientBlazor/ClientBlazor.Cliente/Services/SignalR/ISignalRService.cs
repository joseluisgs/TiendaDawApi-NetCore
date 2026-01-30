namespace ClientBlazor.Cliente.Services.SignalR;

/// <summary>
/// Define el contrato para el servicio de comunicación mediante Hubs de SignalR.
/// Gestiona la conexión persistente, reconexiones automáticas y suscripción a métodos del servidor.
/// </summary>
public interface ISignalRService
{
    /// <summary>
    /// Indica si la conexión con el Hub de SignalR está actualmente activa.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Evento que se dispara cuando llega una notificación desde cualquier método suscrito del Hub.
    /// </summary>
    event Action<string>? OnMessageReceived;

    /// <summary>
    /// Conecta al Hub de productos para recibir eventos de catálogo en tiempo real.
    /// </summary>
    Task ConnectProductosAsync();

    /// <summary>
    /// Conecta al Hub de pedidos para recibir eventos de ventas. Requiere JWT.
    /// </summary>
    Task ConnectPedidosAsync();

    /// <summary>
    /// Cierra la conexión activa con el Hub y libera los recursos.
    /// </summary>
    Task DisconnectAsync();
}