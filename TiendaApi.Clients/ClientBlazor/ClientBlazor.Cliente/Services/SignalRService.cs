using ClientBlazor.Cliente.State;
using System.Text.Json;

namespace ClientBlazor.Cliente.Services;

/// <summary>
/// Servicio SignalR simulado - simula conexiones SignalR Hub.
/// Eventos llegan automáticamente al conectarse, sin suscripciones específicas.
/// Soporta productos (público) y pedidos (con JWT).
/// </summary>
public class SignalRService(
    /// <summary>
    /// Store de autenticación para obtener tokens JWT.
    /// </summary>
    AuthStore authStore,
    /// <summary>
    /// Store de notificaciones para mostrar mensajes.
    /// </summary>
    NotificationStore notificationStore)
{
    private CancellationTokenSource? _cts;
    private Task? _simulationTask;
    private string _currentHub = "productos";
    private readonly Random _random = new Random();

    /// <summary>
    /// Indica si hay una simulación SignalR activa.
    /// </summary>
    public bool IsConnected => _simulationTask != null && !_simulationTask.IsCompleted;

    /// <summary>
    /// Evento que se dispara cuando llega un mensaje.
    /// </summary>
    public event Action<string>? OnMessageReceived;

    /// <summary>
    /// Conecta al Hub de productos (público).
    /// Eventos llegan automáticamente al conectarse.
    /// </summary>
    public async Task ConnectProductosAsync()
    {
        await DisconnectAsync();
        _currentHub = "productos";
        await StartSimulationAsync();
    }

    /// <summary>
    /// Conecta al Hub de pedidos (requiere JWT).
    /// Eventos llegan automáticamente al conectarse.
    /// </summary>
    public async Task ConnectPedidosAsync()
    {
        await DisconnectAsync();

        var token = authStore.GetState().Token;
        if (string.IsNullOrEmpty(token))
        {
            notificationStore.Warning("Se requiere autenticación JWT para SignalR de pedidos");
            return;
        }

        _currentHub = "pedidos";
        await StartSimulationAsync();
    }

    /// <summary>
    /// Desconecta el Hub simulado actual.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts = null;
        }

        if (_simulationTask != null)
        {
            await Task.WhenAny(_simulationTask, Task.Delay(1000)); // Esperar máximo 1 segundo
            _simulationTask = null;
        }

        notificationStore.Info("SignalR desconectado");
    }

    /// <summary>
    /// Inicia la simulación de SignalR.
    /// </summary>
    private async Task StartSimulationAsync()
    {
        notificationStore.Info($"Conectando a SignalR Hub simulado: {_currentHub}...");

        // Simular delay de conexión
        await Task.Delay(_random.Next(800, 2000));

        // Simular conexión exitosa
        notificationStore.Success($"SignalR Hub simulado conectado: {_currentHub}");

        // Iniciar la generación de eventos simulados
        _cts = new CancellationTokenSource();
        _simulationTask = Task.Run(() => GenerateSimulatedEventsAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// Genera eventos simulados continuamente.
    /// </summary>
    private async Task GenerateSimulatedEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Generar un evento inicial de conexión
            var connectionEvent = new
            {
                tipo = "ConexionEstablecida",
                hub = _currentHub,
                connectionId = $"simulated-{_random.Next(1000, 9999)}",
                timestamp = DateTime.UtcNow
            };

            var connectionMessage = JsonSerializer.Serialize(connectionEvent);
            OnMessageReceived?.Invoke(connectionMessage);

            // Generar eventos periódicos
            while (!cancellationToken.IsCancellationRequested)
            {
                // Esperar entre 4-9 segundos entre eventos
                await Task.Delay(_random.Next(4000, 9000), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                var simulatedEvent = GenerateRandomEvent();
                var message = JsonSerializer.Serialize(simulatedEvent);
                OnMessageReceived?.Invoke(message);
            }
        }
        catch (OperationCanceledException)
        {
            // Simulación cancelada normalmente
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                notificationStore.Error($"Error en simulación SignalR: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Genera un evento simulado aleatorio según el hub.
    /// </summary>
    private object GenerateRandomEvent()
    {
        if (_currentHub == "productos")
        {
            var eventosProductos = new[]
            {
                "ProductoCreado",
                "ProductoActualizado",
                "ProductoEliminado",
                "StockBajo"
            };

            var evento = eventosProductos[_random.Next(eventosProductos.Length)];

            return evento switch
            {
                "ProductoCreado" => new
                {
                    productoId = _random.Next(1000, 9999),
                    nombre = $"Nuevo Producto {_random.Next(100, 999)}",
                    precio = _random.Next(10, 500),
                    stock = _random.Next(1, 50),
                    tipo = "PRODUCTO_CREADO",
                    timestamp = DateTime.UtcNow
                },
                "ProductoActualizado" => new
                {
                    productoId = _random.Next(1, 8), // IDs existentes
                    nombre = "Producto Actualizado",
                    precio = _random.Next(50, 300),
                    stock = _random.Next(5, 100),
                    tipo = "PRODUCTO_ACTUALIZADO",
                    timestamp = DateTime.UtcNow
                },
                "ProductoEliminado" => new
                {
                    productoId = _random.Next(1, 8),
                    tipo = "PRODUCTO_ELIMINADO",
                    timestamp = DateTime.UtcNow
                },
                "StockBajo" => new
                {
                    productoId = _random.Next(1, 8),
                    nombreProducto = $"Producto {_random.Next(1, 8)}",
                    stockActual = _random.Next(1, 5),
                    tipo = "STOCK_BAJO",
                    timestamp = DateTime.UtcNow
                },
                _ => new { tipo = "EVENTO_DESCONOCIDO", timestamp = DateTime.UtcNow }
            };
        }
        else // pedidos
        {
            var eventosPedidos = new[]
            {
                "PedidoCreado",
                "PedidoActualizado"
            };

            var evento = eventosPedidos[_random.Next(eventosPedidos.Length)];

            return evento switch
            {
                "PedidoCreado" => new
                {
                    pedidoId = $"PED-{_random.Next(100, 999)}",
                    userId = _random.Next(1, 10),
                    estado = "PENDIENTE",
                    total = _random.Next(50, 1000),
                    tipo = "PEDIDO_CREADO",
                    timestamp = DateTime.UtcNow
                },
                "PedidoActualizado" => new
                {
                    pedidoId = $"PED-{_random.Next(100, 999)}",
                    userId = _random.Next(1, 10),
                    estado = _random.Next(2) == 0 ? "CONFIRMADO" : "ENVIADO",
                    total = _random.Next(50, 1000),
                    tipo = "PEDIDO_ACTUALIZADO",
                    timestamp = DateTime.UtcNow
                },
                _ => new { tipo = "EVENTO_DESCONOCIDO", timestamp = DateTime.UtcNow }
            };
        }
    }
}