using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Tests.Unit.Services.Pedidos;

/// <summary>
/// 🎓 Tests de paridad de canales en los efectos de pedidos: cada evento de
/// tiempo real debe salir SIEMPRE por WebSocket Y SignalR (los métodos
/// combinados), y por SignalR a los grupos del dueño y de administradores —
/// nunca a Clients.All. Regresión de los puntos que antes emitían un solo
/// canal y del bug de broadcast.
/// </summary>
public class PedidoEffectsTests
{
    private const long UserId = 1;

    private Mock<PedidosWebSocketHandler> _webSocket = null!;
    private Mock<IHubContext<PedidosHub>> _hubContext = null!;
    private Mock<IHubClients> _clients = null!;
    private Mock<IClientProxy> _userProxy = null!;
    private Mock<IClientProxy> _adminsProxy = null!;
    private PedidoEffects _effects = null!;

    [SetUp]
    public void Setup()
    {
        var configuration = new Mock<IConfiguration>();
        var wsSection = new Mock<IConfigurationSection>();
        wsSection.Setup(s => s.Value).Returns("5");
        configuration.Setup(c => c.GetSection("WebSocket:RoleCacheTTLMinutes")).Returns(wsSection.Object);

        _webSocket = new Mock<PedidosWebSocketHandler>(
            Mock.Of<ILogger<PedidosWebSocketHandler>>(),
            Mock.Of<IJwtTokenExtractor>(),
            Mock.Of<IJwtService>(),
            Mock.Of<ICacheService>(),
            configuration.Object);

        _userProxy = new Mock<IClientProxy>();
        _adminsProxy = new Mock<IClientProxy>();
        _clients = new Mock<IHubClients>();
        _clients.Setup(c => c.Group($"user-{UserId}")).Returns(_userProxy.Object);
        _clients.Setup(c => c.Group("admins")).Returns(_adminsProxy.Object);
        _hubContext = new Mock<IHubContext<PedidosHub>>();
        _hubContext.Setup(h => h.Clients).Returns(_clients.Object);

        _effects = new PedidoEffects(
            _webSocket.Object,
            _hubContext.Object,
            Mock.Of<IEmailService>(),
            configuration.Object,
            Mock.Of<ILogger<PedidoEffects>>());
    }

    private static PedidoDto UnPedido() => new(
        Id: "PED-2024-0001",
        UserId: UserId,
        Destinatario: null!,
        Items: new List<PedidoItemDto>(),
        Total: 99.5m,
        Estado: "Pendiente",
        DireccionEnvio: null,
        CreatedAt: DateTime.UtcNow);

    /// <summary>
    /// Los envíos son fire-and-forget (Task.Run): se reintenta la verificación
    /// hasta 2s para no depender de tiempos de ejecución.
    /// </summary>
    private static async Task Eventually(Action verify)
    {
        Exception? ultimoError = null;
        for (var intento = 0; intento < 200; intento++)
        {
            try
            {
                verify();
                return;
            }
            catch (Exception ex)
            {
                ultimoError = ex;
                await Task.Delay(10);
            }
        }
        throw ultimoError!;
    }

    [Test]
    public async Task NotificarPedidoCreado_EmitePorWebSocketYSignalR()
    {
        _effects.NotificarPedidoCreado("PED-2024-0001", UserId, "Pendiente", UnPedido());

        await Eventually(() =>
        {
            _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
                It.Is<PedidoNotificacion>(n =>
                    n.Tipo == PedidoNotificationType.CREADO &&
                    n.PedidoId == "PED-2024-0001")), Times.Once);
            _userProxy.Verify(p => p.SendCoreAsync("PedidoCreado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
            _adminsProxy.Verify(p => p.SendCoreAsync("PedidoCreado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
            _clients.Verify(c => c.All, Times.Never);
        });
    }

    [Test]
    public async Task NotificarPedidoActualizado_EmitePorWebSocketYSignalR()
    {
        _effects.NotificarPedidoActualizado("PED-2024-0001", UserId, "Enviado", UnPedido());

        await Eventually(() =>
        {
            _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
                It.Is<PedidoNotificacion>(n =>
                    n.Tipo == PedidoNotificationType.ESTADO_ACTUALIZADO &&
                    n.Estado == "Enviado")), Times.Once);
            _userProxy.Verify(p => p.SendCoreAsync("PedidoActualizado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
            _adminsProxy.Verify(p => p.SendCoreAsync("PedidoActualizado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
            _clients.Verify(c => c.All, Times.Never);
        });
    }

    [Test]
    public async Task NotificarPedidoEliminado_EmitePorWebSocketYSignalR()
    {
        _effects.NotificarPedidoEliminado("PED-2024-0001", UserId, "CANCELADO", 99.5m);

        await Eventually(() =>
        {
            _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
                It.Is<PedidoNotificacion>(n =>
                    n.Tipo == PedidoNotificationType.ELIMINADO &&
                    n.Estado == "CANCELADO")), Times.Once);
            _userProxy.Verify(p => p.SendCoreAsync("PedidoEliminado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
            _adminsProxy.Verify(p => p.SendCoreAsync("PedidoEliminado", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
            _clients.Verify(c => c.All, Times.Never);
        });
    }

    [Test]
    public async Task NotificarPedidoEliminado_DiferenciaAlDueñoYAlGrupoDeAdmins()
    {
        _effects.NotificarPedidoEliminado("PED-2024-0001", UserId, "CANCELADO", 99.5m);

        await Eventually(() =>
        {
            // Solo los dos grupos selectivos: jamás broadcast a Clients.All.
            _clients.Verify(c => c.Group($"user-{UserId}"), Times.AtLeastOnce);
            _clients.Verify(c => c.Group("admins"), Times.AtLeastOnce);
            _clients.Verify(c => c.All, Times.Never);
        });
    }
}
