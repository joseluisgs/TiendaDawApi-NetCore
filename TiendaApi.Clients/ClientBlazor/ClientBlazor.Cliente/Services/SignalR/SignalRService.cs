using Microsoft.AspNetCore.SignalR.Client;
using ClientBlazor.Cliente.Configuration;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;

namespace ClientBlazor.Cliente.Services.SignalR;

/// <inheritdoc cref="ISignalRService" />
public class SignalRService(IAuthStore authStore, INotificationStore notificationStore) : ISignalRService, IAsyncDisposable
{
    private HubConnection? _hubConnection;
    /// <inheritdoc />
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    /// <inheritdoc />
    public event Action<string>? OnMessageReceived;

    /// <inheritdoc cref="ISignalRService.ConnectProductosAsync" />
    public async Task ConnectProductosAsync()
    {
        var url = $"{AppConfig.ApiBaseUrl}/hubs/productos";
        await BuildAndStartConnection(url);
    }

    /// <inheritdoc cref="ISignalRService.ConnectPedidosAsync" />
    public async Task ConnectPedidosAsync()
    {
        var token = authStore.GetState().Token;
        if (string.IsNullOrEmpty(token))
        {
            notificationStore.Warning("Inicia sesión para conectar al Hub de pedidos");
            OnMessageReceived?.Invoke("Error: Se requiere autenticación para pedidos");
            return;
        }

        var url = $"{AppConfig.ApiBaseUrl}/hubs/pedidos";
        await BuildAndStartConnection(url, token);
    }

    /// <summary>
    /// Configura e inicia la conexión con el Hub de SignalR.
    /// </summary>
    /// <param name="url">URL base del Hub.</param>
    /// <param name="token">Token JWT opcional para la autenticación.</param>
    private async Task BuildAndStartConnection(string url, string? token = null)
    {
        if (IsConnected) await DisconnectAsync();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(url, options => { if (token != null) options.AccessTokenProvider = () => Task.FromResult<string?>(token); })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        try
        {
            await _hubConnection.StartAsync();
            notificationStore.Success("Conectado al Hub de SignalR");
            OnMessageReceived?.Invoke($"Conectado al Hub: {url}");
        }
        catch (Exception ex)
        {
            notificationStore.Error($"Error de conexión SignalR: {ex.Message}", "Fallo de Hub");
            OnMessageReceived?.Invoke($"Error conectando al Hub: {ex.Message}");
        }
    }

    /// <summary>
    /// Registra los escuchadores para los eventos publicados por el Hub del servidor.
    /// </summary>
    private void RegisterHandlers()
    {
        if (_hubConnection == null) return;
        _hubConnection.On<object>("ProductoCreado", (data) => OnMessageReceived?.Invoke($"📦 [ProductoCreado] {data}"));
        _hubConnection.On<object>("ProductoActualizado", (data) => OnMessageReceived?.Invoke($"📦 [ProductoActualizado] {data}"));
        _hubConnection.On<object>("ProductoEliminado", (id) => OnMessageReceived?.Invoke($"📦 [ProductoEliminado] ID: {id}"));
        _hubConnection.On<object>("StockBajo", (data) => OnMessageReceived?.Invoke($"⚠️ [StockBajo] {data}"));
        _hubConnection.On<object>("PedidoCreado", (data) => OnMessageReceived?.Invoke($"🛒 [PedidoCreado] {data}"));
        _hubConnection.On<object>("PedidoActualizado", (data) => OnMessageReceived?.Invoke($"🛒 [PedidoActualizado] {data}"));
    }

    /// <inheritdoc cref="ISignalRService.DisconnectAsync" />
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
            notificationStore.Info("Hub desconectado");
            OnMessageReceived?.Invoke("Hub desconectado");
        }
    }

    /// <summary>
    /// Libera los recursos de conexión de forma asíncrona.
    /// </summary>
    /// <returns>Tarea de eliminación.</returns>
    public async ValueTask DisposeAsync() => await DisconnectAsync();
}