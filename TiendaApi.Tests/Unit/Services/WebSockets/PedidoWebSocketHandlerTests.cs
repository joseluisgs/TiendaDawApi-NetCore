using System.Net.WebSockets;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Tests.Unit.Services.WebSockets;

/// <summary>
/// Tests unitarios para PedidosWebSocketHandler.
/// Verifica el comportamiento de las notificaciones selectivas por usuario y el sistema de caché.
/// </summary>
public class PedidosWebSocketHandlerTests
{
    private readonly Mock<ILogger<PedidosWebSocketHandler>> _mockLogger;
    private readonly Mock<IJwtTokenExtractor> _mockTokenExtractor;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly PedidosWebSocketHandler _handler;

    public PedidosWebSocketHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PedidosWebSocketHandler>>();
        _mockTokenExtractor = new Mock<IJwtTokenExtractor>();
        _mockCacheService = new Mock<ICacheService>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Configurar TTL por defecto para tests (5 minutos)
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.Value).Returns("5");
        _mockConfiguration.Setup(c => c.GetSection("WebSocket:RoleCacheTTLMinutes"))
            .Returns(sectionMock.Object);

        _handler = new PedidosWebSocketHandler(
            _mockLogger.Object,
            _mockTokenExtractor.Object,
            _mockCacheService.Object,
            _mockConfiguration.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var handler = CreateHandler();

        // Assert
        Assert.That(handler.GetConnectionCount(), Is.EqualTo(0));
    }

    [Test]
    public void Constructor_SetsUpDependenciesCorrectly()
    {
        // Arrange & Act
        var logger = new Mock<ILogger<PedidosWebSocketHandler>>();
        var tokenExtractor = new Mock<IJwtTokenExtractor>();
        var cacheService = new Mock<ICacheService>();
        var config = new Mock<IConfiguration>();

        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s.Value).Returns("5");
        config.Setup(c => c.GetSection("WebSocket:RoleCacheTTLMinutes"))
            .Returns(sectionMock.Object);

        // Act
        var handler = new PedidosWebSocketHandler(
            logger.Object,
            tokenExtractor.Object,
            cacheService.Object,
            config.Object);

        // Assert
        Assert.That(handler, Is.Not.Null);
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

    #region PedidoNotificacion Tests

    private const string NotificationTypeCreated = "PEDIDO_CREATED";
    private const string NotificationTypeEstadoUpdated = "PEDIDO_ESTADO_UPDATED";

    [Test]
    public void PedidoNotificacion_CanBeCreatedWithAllFields()
    {
        // Arrange & Act
        var notificacion = new PedidoNotificacion(
            NotificationTypeCreated,
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
            NotificationTypeCreated,
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

    private PedidosWebSocketHandler CreateHandler()
    {
        return new PedidosWebSocketHandler(
            _mockLogger.Object,
            _mockTokenExtractor.Object,
            _mockCacheService.Object,
            _mockConfiguration.Object);
    }

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
