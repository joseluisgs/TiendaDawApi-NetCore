using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Publishers;
using TiendaApi.Api.Realtime.Productos;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Tests.Unit.Services.Productos;

/// <summary>
///     🎓 Tests de paridad de canales en los efectos de productos: cada evento de
///     tiempo real debe salir SIEMPRE por WebSocket Y SignalR (los métodos
///     combinados). Antes, la actualización por imagen/PATCH solo emitía por
///     WebSocket y se perdía el evento SignalR (incluido el mutation GraphQL que
///     delega en UpdatePartialAsync).
/// </summary>
public class ProductoEffectsTests {
    private Mock<IClientProxy> _allProxy = null!;
    private Mock<IHubClients> _clients = null!;
    private ProductoEffects _effects = null!;
    private Mock<IHubContext<ProductosHub>> _hubContext = null!;
    private Mock<ProductosWebSocketHandler> _webSocket = null!;

    [SetUp]
    public void Setup() {
        _webSocket = new Mock<ProductosWebSocketHandler>(
            Mock.Of<ILogger<ProductosWebSocketHandler>>());

        // Los productos son catálogo público: broadcast en ambos canales.
        _allProxy = new Mock<IClientProxy>();
        _clients = new Mock<IHubClients>();
        _clients.Setup(c => c.All).Returns(_allProxy.Object);
        _hubContext = new Mock<IHubContext<ProductosHub>>();
        _hubContext.Setup(h => h.Clients).Returns(_clients.Object);

        _effects = new ProductoEffects(
            _webSocket.Object,
            _hubContext.Object,
            Mock.Of<IEmailService>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<ProductoEffects>>());
    }

    private static ProductoDto UnProducto() {
        return new ProductoDto(
            42,
            "Teclado",
            "Mecánico",
            25.5m,
            10,
            null,
            null!,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }

    /// <summary>
    ///     Los envíos son fire-and-forget (Task.Run): se reintenta la verificación
    ///     hasta 2s para no depender de tiempos de ejecución.
    /// </summary>
    private static async Task Eventually(Action verify) {
        Exception? ultimoError = null;
        for (int intento = 0; intento < 200; intento++) {
            try {
                verify();
                return;
            } catch (Exception ex) {
                ultimoError = ex;
                await Task.Delay(10);
            }
        }

        throw ultimoError!;
    }

    [Test]
    public async Task NotificarProductoCreado_EmitePorWebSocketYSignalR() {
        _effects.NotificarProductoCreado(UnProducto());

        await Eventually(() => {
            _webSocket.Verify(h => h.NotifyAsync(
                It.Is<ProductoNotificacion>(n => n.ProductoId == 42)), Times.Once);
            _allProxy.Verify(
                p => p.SendCoreAsync("ProductoCreado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Once);
        });
    }

    [Test]
    public async Task NotificarProductoActualizado_EmitePorWebSocketYSignalR() {
        _effects.NotificarProductoActualizado(UnProducto());

        await Eventually(() => {
            _webSocket.Verify(h => h.NotifyAsync(
                It.Is<ProductoNotificacion>(n => n.ProductoId == 42)), Times.Once);
            _allProxy.Verify(
                p => p.SendCoreAsync("ProductoActualizado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Once);
        });
    }

    [Test]
    public async Task NotificarProductoEliminado_EmitePorWebSocketYSignalR() {
        _effects.NotificarProductoEliminado(42);

        await Eventually(() => {
            _webSocket.Verify(h => h.NotifyAsync(
                It.Is<ProductoNotificacion>(n => n.ProductoId == 42)), Times.Once);
            _allProxy.Verify(
                p => p.SendCoreAsync("ProductoEliminado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Once);
        });
    }
}
