using System.Net.WebSockets;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
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
    private readonly PedidoWebSocketHandler _handler;

    public PedidoWebSocketHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PedidoWebSocketHandler>>();
        _handler = new PedidoWebSocketHandler(_mockLogger.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var handler = new PedidoWebSocketHandler(_mockLogger.Object);

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

    #region ExtractUserInfoFromToken Tests

    [Test]
    public void ExtractUserInfoFromToken_WithValidToken_ReturnsUserIdAndRole()
    {
        // Arrange - Create token con rol de cliente
        var (token, userId) = CreateValidJwtToken(123L, "cliente");

        // Act
        var (extractedUserId, isAdmin) = ExtractUserInfo(token);

        // Assert
        Assert.That(extractedUserId, Is.EqualTo(userId));
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void ExtractUserInfoFromToken_WithAdminRole_ReturnsIsAdminTrue()
    {
        // Arrange - Create token con rol de admin
        var (token, userId) = CreateValidJwtToken(1L, "admin");

        // Act
        var (extractedUserId, isAdmin) = ExtractUserInfo(token);

        // Assert
        Assert.That(extractedUserId, Is.EqualTo(userId));
        Assert.That(isAdmin, Is.True);
    }

    [Test]
    public void ExtractUserInfoFromToken_WithAdminRoleUppercase_ReturnsIsAdminTrue()
    {
        // Arrange - Create token con rol de ADMIN (mayúsculas)
        var (token, userId) = CreateValidJwtToken(1L, "ADMIN");

        // Act
        var (extractedUserId, isAdmin) = ExtractUserInfo(token);

        // Assert
        Assert.That(extractedUserId, Is.EqualTo(userId));
        Assert.That(isAdmin, Is.True);
    }

    [Test]
    public void ExtractUserInfoFromToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token.here";

        // Act
        var (userId, isAdmin) = ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void ExtractUserInfoFromToken_WithEmptyToken_ReturnsNull()
    {
        // Arrange
        var token = "";

        // Act
        var (userId, isAdmin) = ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void ExtractUserInfoFromToken_WithMalformedToken_ReturnsNull()
    {
        // Arrange
        var token = "not-a-jwt";

        // Act
        var (userId, isAdmin) = ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void ExtractUserInfoFromToken_WithMissingRoleClaim_ReturnsIsAdminFalse()
    {
        // Arrange - Token sin rol
        var token = CreateTokenWithoutRole(123L);

        // Act
        var (userId, isAdmin) = ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.EqualTo(123L));
        Assert.That(isAdmin, Is.False);
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

    private static (string Token, long UserId) CreateValidJwtToken(long userId, string role)
    {
        var handler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(1)
        };
        
        var token = handler.CreateToken(tokenDescriptor);
        return (handler.WriteToken(token), userId);
    }

    private static string CreateTokenWithoutRole(long userId)
    {
        var handler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1)
        };
        
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private (long? UserId, bool IsAdmin) ExtractUserInfo(string token)
    {
        var method = typeof(PedidoWebSocketHandler)
            .GetMethod("ExtractUserInfoFromToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method!.Invoke(_handler, new[] { token });
        var tuple = (System.ValueTuple<long?, bool>)result!;
        return (tuple.Item1, tuple.Item2);
    }

    #endregion
}
