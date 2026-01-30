namespace ClientBlazor.Cliente.Services.Websocket;

/// <summary>
/// Define el contrato para el servicio de comunicación persistente mediante WebSockets nativos.
/// Ideal para notificaciones de bajo nivel y flujos de datos continuos.
/// </summary>
public interface IWebSocketService
{
    /// <summary>
    /// Indica si la conexión con el servidor de sockets está actualmente abierta.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Evento que se dispara cada vez que se recibe un mensaje de texto del servidor.
    /// </summary>
    event Action<string>? OnMessageReceived;

    /// <summary>
    /// Inicia la conexión con el endpoint público de eventos de productos.
    /// </summary>
    Task ConnectProductosAsync();

    /// <summary>
    /// Inicia la conexión con el endpoint protegido de eventos de pedidos.
    /// Requiere un token JWT válido.
    /// </summary>
    Task ConnectPedidosAsync();

    /// <summary>
    /// Finaliza la conexión activa y libera los recursos asociados.
    /// </summary>
    Task DisconnectAsync();
}