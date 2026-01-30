using System.Net.WebSockets;
using System.Text;
using ClientBlazor.Cliente.Configuration;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;

namespace ClientBlazor.Cliente.Services.Websocket;

/// <inheritdoc cref="IWebSocketService" />
public class WebSocketService(IAuthStore authStore, INotificationStore notificationStore) : IWebSocketService, IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    
    /// <inheritdoc />
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;
    /// <inheritdoc />
    public event Action<string>? OnMessageReceived;

    /// <inheritdoc cref="IWebSocketService.ConnectProductosAsync" />
    public async Task ConnectProductosAsync()
    {
        var url = $"{AppConfig.ApiBaseUrl}/ws/productos".Replace("http", "ws");
        await ConnectAsync(url);
    }

    /// <inheritdoc cref="IWebSocketService.ConnectPedidosAsync" />
    public async Task ConnectPedidosAsync()
    {
        var token = authStore.GetState().Token;
        if (string.IsNullOrEmpty(token))
        {
            notificationStore.Warning("Inicia sesión para conectar al WebSocket de pedidos");
            OnMessageReceived?.Invoke("Error: Se requiere autenticación para pedidos");
            return;
        }

        var url = $"{AppConfig.ApiBaseUrl}/ws/pedidos?token={token}".Replace("http", "ws");
        await ConnectAsync(url);
    }

    /// <summary>
    /// Establece la conexión física con el servidor de sockets.
    /// </summary>
    /// <param name="url">URL completa del WebSocket (ws/wss).</param>
    private async Task ConnectAsync(string url)
    {
        if (IsConnected) await DisconnectAsync();

        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        try
        {
            await _webSocket.ConnectAsync(new Uri(url), _cts.Token);
            _ = ReceiveLoopAsync(_cts.Token);
            notificationStore.Success("Conexión WebSocket establecida");
            OnMessageReceived?.Invoke($"Conectado a {url}");
        }
        catch (Exception ex)
        {
            notificationStore.Error($"Fallo al conectar WebSocket: {ex.Message}", "Error de Conexión");
            OnMessageReceived?.Invoke($"Error de conexión: {ex.Message}");
            _webSocket?.Dispose();
            _webSocket = null;
        }
    }

    /// <inheritdoc cref="IWebSocketService.DisconnectAsync" />
    public async Task DisconnectAsync()
    {
        if (_webSocket == null) return;
        try
        {
            if (_webSocket.State == WebSocketState.Open)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre solicitado", CancellationToken.None);
        }
        finally
        {
            _cts?.Cancel();
            _webSocket.Dispose();
            _webSocket = null;
            notificationStore.Info("WebSocket desconectado");
            OnMessageReceived?.Invoke("Desconectado del servidor");
        }
    }

    /// <summary>
    /// Bucle infinito de recepción de mensajes asíncronos.
    /// </summary>
    /// <param name="ct">Token para detener el bucle.</param>
    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close) await DisconnectAsync();
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessageReceived?.Invoke(message);
                }
            }
        }
        catch (Exception ex)
        {
            notificationStore.Error("Se ha perdido la conexión WebSocket", "Desconexión Inesperada");
            OnMessageReceived?.Invoke($"Error en recepción: {ex.Message}");
            await DisconnectAsync();
        }
    }

    /// <summary>
    /// Libera los recursos del sistema.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _webSocket?.Dispose();
    }
}