using ClientBlazor.Cliente.State;
using System.Text.Json;

namespace ClientBlazor.Cliente.Services;

/// <summary>
/// Servicio WebSocket simulado - simula conexiones WebSocket nativas.
/// Eventos llegan automáticamente al conectarse, sin suscripciones específicas.
/// Soporta productos (público) y pedidos (con JWT).
/// </summary>
public class WebSocketService(
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
    private string _currentType = "productos";
    private readonly Random _random = new();

    /// <summary>
    /// Indica si hay una conexión WebSocket simulada activa.
    /// </summary>
    public bool IsConnected => _simulationTask != null && !_simulationTask.IsCompleted;

    /// <summary>
    /// Evento que se dispara cuando llega un mensaje simulado.
    /// </summary>
    public event Action<string>? OnMessageReceived;

    /// <summary>
    /// Conecta al WebSocket simulado de productos (público).
    /// Eventos llegan automáticamente al conectarse.
    /// </summary>
    public async Task ConnectProductosAsync()
    {
        await DisconnectAsync();
        _currentType = "productos";
        await StartSimulationAsync();
    }

    /// <summary>
    /// Conecta al WebSocket simulado de pedidos (requiere JWT).
    /// Eventos llegan automáticamente al conectarse.
    /// </summary>
    public async Task ConnectPedidosAsync()
    {
        await DisconnectAsync();

        var token = authStore.GetState().Token;
        if (string.IsNullOrEmpty(token))
        {
            notificationStore.Warning("Se requiere autenticación JWT para WebSocket de pedidos");
            return;
        }

        _currentType = "pedidos";
        await StartSimulationAsync();
    }

    /// <summary>
    /// Desconecta el WebSocket simulado actual.
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

        notificationStore.Info("WebSocket desconectado");
    }

    /// <summary>
    /// Inicia la simulación de eventos WebSocket.
    /// </summary>
    private async Task StartSimulationAsync()
    {
        notificationStore.Info($"Conectando a WebSocket simulado: {_currentType}...");

        // Simular delay de conexión
        await Task.Delay(_random.Next(500, 1500));

        notificationStore.Success($"WebSocket simulado conectado: {_currentType}");

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
                timestamp = DateTime.UtcNow,
                tipoConexion = _currentType
            };

            var connectionMessage = JsonSerializer.Serialize(connectionEvent);
            OnMessageReceived?.Invoke(connectionMessage);

            // Generar eventos periódicos
            while (!cancellationToken.IsCancellationRequested)
            {
                // Esperar entre 3-8 segundos entre eventos
                await Task.Delay(_random.Next(3000, 8000), cancellationToken);

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
                notificationStore.Error($"Error en simulación WebSocket: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Genera un evento simulado aleatorio según el tipo de conexión.
    /// </summary>
    private object GenerateRandomEvent()
    {
        if (_currentType == "productos")
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
                    tipo = "ProductoCreado",
                    producto = new
                    {
                        id = _random.Next(1000, 9999),
                        nombre = $"Nuevo Producto {_random.Next(100, 999)}",
                        precio = _random.Next(10, 500),
                        stock = _random.Next(1, 50)
                    },
                    timestamp = DateTime.UtcNow
                },
                "ProductoActualizado" => new
                {
                    tipo = "ProductoActualizado",
                    producto = new
                    {
                        id = _random.Next(1, 8), // IDs existentes
                        nombre = "Producto Actualizado",
                        precio = _random.Next(50, 300),
                        stock = _random.Next(5, 100)
                    },
                    timestamp = DateTime.UtcNow
                },
                "ProductoEliminado" => new
                {
                    tipo = "ProductoEliminado",
                    productoId = _random.Next(1, 8),
                    timestamp = DateTime.UtcNow
                },
                "StockBajo" => new
                {
                    tipo = "StockBajo",
                    productoId = _random.Next(1, 8),
                    nombreProducto = $"Producto {_random.Next(1, 8)}",
                    stockActual = _random.Next(1, 5),
                    timestamp = DateTime.UtcNow
                },
                _ => new { tipo = "EventoDesconocido", timestamp = DateTime.UtcNow }
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
                    tipo = "PedidoCreado",
                    pedido = new
                    {
                        id = $"P-{DateTime.Now:yyyyMMdd}-{_random.Next(100, 999)}",
                        userId = _random.Next(1, 10),
                        estado = "PENDIENTE",
                        total = _random.Next(50, 1000)
                    },
                    timestamp = DateTime.UtcNow
                },
                "PedidoActualizado" => new
                {
                    tipo = "PedidoActualizado",
                    pedido = new
                    {
                        id = $"P-{DateTime.Now:yyyyMMdd}-{_random.Next(100, 999)}",
                        userId = _random.Next(1, 10),
                        estado = _random.Next(2) == 0 ? "CONFIRMADO" : "ENVIADO",
                        total = _random.Next(50, 1000)
                    },
                    timestamp = DateTime.UtcNow
                },
                _ => new { tipo = "EventoDesconocido", timestamp = DateTime.UtcNow }
            };
        }
    }
}