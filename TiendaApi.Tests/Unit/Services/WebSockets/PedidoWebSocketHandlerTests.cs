using System.Net.WebSockets;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Services.Auth;
using TiendaApi.Apis.WebSockets.Pedidos;

namespace TiendaApi.Tests.Unit.Services.WebSockets;

/// <summary>
/// Tests unitarios para PedidoWebSocketHandler.
/// Verifica el comportamiento de las notificaciones selectivas por usuario.
/// </summary>
public class PedidoWebSocketHandlerTests
{
    private readonly Mock<ILogger<PedidoWebSocketHandler>> _mockLogger;
    private readonly Mock<IJwtTokenExtractor> _mockTokenExtractor;
    private readonly PedidoWebSocketHandler _handler;

    public PedidoWebSocketHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PedidoWebSocketHandler>>();
        _mockTokenExtractor = new Mock<IJwtTokenExtractor>();
        
        _handler = new PedidoWebSocketHandler(_mockLogger.Object, _mockTokenExtractor.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var handler = new PedidoWebSocketHandler(_mockLogger.Object, _mockTokenExtractor.Object);

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

    #region JwtTokenExtractor Integration Tests

    [Test]
    public void JwtTokenExtractor_ExtractUserInfo_WithValidToken_ReturnsUserIdAndRole()
    {
        // Arrange - Create real JWT token
        var token = CreateValidJwtToken(123L, "cliente");

        // Act
        var (userId, isAdmin, role) = ExtractUserInfoFromToken(token);

        // Assert
        Assert.That(userId, Is.EqualTo(123L));
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.EqualTo("cliente"));
    }

    [Test]
    public void JwtTokenExtractor_ExtractUserInfo_WithAdminRole_ReturnsIsAdminTrue()
    {
        // Arrange
        var token = CreateValidJwtToken(1L, "admin");

        // Act
        var (userId, isAdmin, role) = ExtractUserInfoFromToken(token);

        // Assert
        Assert.That(userId, Is.EqualTo(1L));
        Assert.That(isAdmin, Is.True);
        Assert.That(role, Is.EqualTo("admin"));
    }

    [Test]
    public void JwtTokenExtractor_ExtractUserInfo_WithAdminRoleUppercase_ReturnsIsAdminTrue()
    {
        // Arrange
        var token = CreateValidJwtToken(1L, "ADMIN");

        // Act
        var (userId, isAdmin, role) = ExtractUserInfoFromToken(token);

        // Assert
        Assert.That(userId, Is.EqualTo(1L));
        Assert.That(isAdmin, Is.True);
        Assert.That(role, Is.EqualTo("ADMIN"));
    }

    [Test]
    public void JwtTokenExtractor_ExtractUserInfo_WithInvalidToken_ReturnsNullUserId()
    {
        // Arrange
        var token = "invalid.token.here";

        // Act
        var (userId, isAdmin, role) = ExtractUserInfoFromToken(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    [Test]
    public void JwtTokenExtractor_ExtractUserInfo_WithEmptyToken_ReturnsNull()
    {
        // Arrange
        var token = "";

        // Act
        var (userId, isAdmin, role) = ExtractUserInfoFromToken(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    [Test]
    public void JwtTokenExtractor_ExtractUserInfo_WithMalformedToken_ReturnsNull()
    {
        // Arrange
        var token = "not-a-jwt";

        // Act
        var (userId, isAdmin, role) = ExtractUserInfoFromToken(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    [Test]
    public void JwtTokenExtractor_ExtractUserInfo_WithMissingRoleClaim_ReturnsIsAdminFalse()
    {
        // Arrange - Token sin rol
        var token = CreateTokenWithoutRole(123L);

        // Act
        var (userId, isAdmin, role) = ExtractUserInfoFromToken(token);

        // Assert
        Assert.That(userId, Is.EqualTo(123L));
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    [Test]
    public void JwtTokenExtractor_ExtractEmail_WithValidToken_ReturnsEmail()
    {
        // Arrange
        var token = CreateTokenWithEmail(123L, "test@example.com");

        // Act
        var email = ExtractEmailFromToken(token);

        // Assert
        Assert.That(email, Is.EqualTo("test@example.com"));
    }

    [Test]
    public void JwtTokenExtractor_IsValidTokenFormat_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = CreateValidJwtToken(123L, "cliente");

        // Act
        var isValid = IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void JwtTokenExtractor_IsValidTokenFormat_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var isValid = IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void JwtTokenExtractor_IsValidTokenFormat_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var token = "";

        // Act
        var isValid = IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void JwtTokenExtractor_ExtractUserId_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var token = CreateValidJwtToken(456L, "cliente");

        // Act
        var userId = ExtractUserIdFromToken(token);

        // Assert
        Assert.That(userId, Is.EqualTo(456L));
    }

    [Test]
    public void JwtTokenExtractor_ExtractRole_WithValidToken_ReturnsRole()
    {
        // Arrange
        var token = CreateValidJwtToken(123L, "admin");

        // Act
        var role = ExtractRoleFromToken(token);

        // Assert
        Assert.That(role, Is.EqualTo("admin"));
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

    private static string CreateValidJwtToken(long userId, string role)
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
        return handler.WriteToken(token);
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

    private static string CreateTokenWithEmail(long userId, string email)
    {
        var handler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email)
            }),
            Expires = DateTime.UtcNow.AddHours(1)
        };
        
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private static (long? UserId, bool IsAdmin, string? Role) ExtractUserInfoFromToken(string token)
    {
        var extractor = new JwtTokenExtractor(Mock.Of<ILogger<JwtTokenExtractor>>());
        return extractor.ExtractUserInfo(token);
    }

    private static string? ExtractEmailFromToken(string token)
    {
        var extractor = new JwtTokenExtractor(Mock.Of<ILogger<JwtTokenExtractor>>());
        return extractor.ExtractEmail(token);
    }

    private static bool IsValidTokenFormat(string token)
    {
        var extractor = new JwtTokenExtractor(Mock.Of<ILogger<JwtTokenExtractor>>());
        return extractor.IsValidTokenFormat(token);
    }

    private static long? ExtractUserIdFromToken(string token)
    {
        var extractor = new JwtTokenExtractor(Mock.Of<ILogger<JwtTokenExtractor>>());
        return extractor.ExtractUserId(token);
    }

    private static string? ExtractRoleFromToken(string token)
    {
        var extractor = new JwtTokenExtractor(Mock.Of<ILogger<JwtTokenExtractor>>());
        return extractor.ExtractRole(token);
    }

    private static bool IsAdminFromToken(string token)
    {
        var extractor = new JwtTokenExtractor(Mock.Of<ILogger<JwtTokenExtractor>>());
        return extractor.IsAdmin(token);
    }

    #endregion
}
