using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.WebSockets.Pedidos;

namespace TiendaApi.Tests.Unit.Services.WebSockets;

/// <summary>
/// Tests unitarios para PedidoWebSocketHandler.
/// Verifica el comportamiento de las notificaciones selectivas por usuario.
/// </summary>
public class PedidoWebSocketHandlerTests
{
    private readonly Mock<ILogger<PedidoWebSocketHandler>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly PedidoWebSocketHandler _handler;

    public PedidoWebSocketHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PedidoWebSocketHandler>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        _mockConfiguration.Setup(c => c["WebSocket:AdminUserIds"]).Returns("1,2");
        
        _handler = new PedidoWebSocketHandler(_mockLogger.Object, _mockConfiguration.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithAdminUserIds_InitializesCorrectly()
    {
        // Arrange & Act
        var handler = new PedidoWebSocketHandler(_mockLogger.Object, _mockConfiguration.Object);

        // Assert
        Assert.That(handler.GetAdminConnectionCount(), Is.EqualTo(0));
        Assert.That(handler.GetConnectionCount(), Is.EqualTo(0));
    }

    #endregion

    #region GetConnectionCount Tests

    [Test]
    public void GetConnectionCount_EmptyConnections_ReturnsZero()
    {
        // Arrange & Act
        var count = _handler.GetConnectionCount();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    #endregion

    #region NotifyUserAsync Tests

    [Test]
    public async Task NotifyUserAsync_WithNoConnections_DoesNotThrow()
    {
        // Arrange
        var notification = new PedidoNotificacion(
            PedidoNotificationType.CREATED,
            "PED-001",
            123L,
            "Pendiente",
            null
        );

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _handler.NotifyUserAsync(123L, notification));
    }

    #endregion

    #region NotifyAdminsAsync Tests

    [Test]
    public async Task NotifyAdminsAsync_WithNoConnections_DoesNotThrow()
    {
        // Arrange
        var notification = new PedidoNotificacion(
            PedidoNotificationType.CREATED,
            "PED-001",
            123L,
            "Pendiente",
            null
        );

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _handler.NotifyAdminsAsync(notification));
    }

    #endregion

    #region NotifyUserAndAdminsAsync Tests

    [Test]
    public async Task NotifyUserAndAdminsAsync_WithNoConnections_DoesNotThrow()
    {
        // Arrange
        var notification = new PedidoNotificacion(
            PedidoNotificationType.ESTADO_UPDATED,
            "PED-001",
            123L,
            "Enviado",
            null
        );

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _handler.NotifyUserAndAdminsAsync(123L, notification));
    }

    #endregion

    #region GetAdminConnectionCount Tests

    [Test]
    public void GetAdminConnectionCount_WithNoConnections_ReturnsZero()
    {
        // Arrange & Act
        var count = _handler.GetAdminConnectionCount();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    #endregion

    #region ExtractUserIdFromToken Tests

    [Test]
    public void ExtractUserIdFromToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token.here";

        // Act
        var userId = ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void ExtractUserIdFromToken_WithEmptyToken_ReturnsNull()
    {
        // Arrange
        var token = "";

        // Act
        var userId = ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void ExtractUserIdFromToken_WithMalformedToken_ReturnsNull()
    {
        // Arrange
        var token = "not-a-jwt";

        // Act
        var userId = ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.Null);
    }

    #endregion

    #region PedidoNotificationType Tests

    [Test]
    public void PedidoNotificationType_HasCorrectValues()
    {
        // Assert
        Assert.That(PedidoNotificationType.CREATED, Is.EqualTo("PEDIDO_CREATED"));
        Assert.That(PedidoNotificationType.ESTADO_UPDATED, Is.EqualTo("PEDIDO_ESTADO_UPDATED"));
    }

    #endregion

    #region PedidoNotificacion Tests

    [Test]
    public void PedidoNotificacion_CanBeCreatedWithAllFields()
    {
        // Arrange & Act
        var notificacion = new PedidoNotificacion(
            PedidoNotificationType.CREATED,
            "PED-001",
            123L,
            "Pendiente",
            new { Total = 99.99 }
        );

        // Assert
        Assert.That(notificacion.Tipo, Is.EqualTo("PEDIDO_CREATED"));
        Assert.That(notificacion.PedidoId, Is.EqualTo("PED-001"));
        Assert.That(notificacion.UserId, Is.EqualTo(123L));
        Assert.That(notificacion.Estado, Is.EqualTo("Pendiente"));
        Assert.That(notificacion.Data, Is.Not.Null);
    }

    [Test]
    public void PedidoNotificacion_CanBeCreatedWithNullData()
    {
        // Arrange & Act
        var notificacion = new PedidoNotificacion(
            PedidoNotificationType.CREATED,
            "PED-001",
            123L,
            "Pendiente",
            null
        );

        // Assert
        Assert.That(notificacion.Data, Is.Null);
    }

    #endregion

    #region Helpers

    private long? ExtractUserId(string token)
    {
        var method = typeof(PedidoWebSocketHandler)
            .GetMethod("ExtractUserIdFromToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        return (long?)method!.Invoke(_handler, new[] { token });
    }

    #endregion
}
