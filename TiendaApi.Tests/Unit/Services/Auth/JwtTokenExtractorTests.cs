using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Tests.Unit.Services.Auth;

/// <summary>
///     Tests unitarios para JwtTokenExtractor.
///     Verifica la extracción de información de tokens JWT.
/// </summary>
public class JwtTokenExtractorTests {
    private readonly JwtTokenExtractor _extractor;
    private readonly Mock<ILogger<JwtTokenExtractor>> _mockLogger;

    public JwtTokenExtractorTests() {
        _mockLogger = new Mock<ILogger<JwtTokenExtractor>>();
        _extractor = new JwtTokenExtractor(_mockLogger.Object);
    }

    #region ExtractUserId Tests

    [Test]
    public void ExtractUserId_WithValidToken_ReturnsUserId() {
        // Arrange
        string token = CreateToken(123L, "cliente");

        // Act
        long? userId = _extractor.ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.EqualTo(123L));
    }

    [Test]
    public void ExtractUserId_WithInvalidToken_ReturnsNull() {
        // Arrange
        string token = "invalid.token";

        // Act
        long? userId = _extractor.ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void ExtractUserId_WithEmptyToken_ReturnsNull() {
        // Arrange
        string token = "";

        // Act
        long? userId = _extractor.ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void ExtractUserId_WithNullToken_ReturnsNull() {
        // Act
        long? userId = _extractor.ExtractUserId(null!);

        // Assert
        Assert.That(userId, Is.Null);
    }

    #endregion

    #region ExtractRole Tests

    [Test]
    public void ExtractRole_WithAdminRole_ReturnsAdmin() {
        // Arrange
        string token = CreateToken(1L, "admin");

        // Act
        string? role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.EqualTo("admin"));
    }

    [Test]
    public void ExtractRole_WithClientRole_ReturnsCliente() {
        // Arrange
        string token = CreateToken(123L, "cliente");

        // Act
        string? role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.EqualTo("cliente"));
    }

    [Test]
    public void ExtractRole_WithMissingRole_ReturnsNull() {
        // Arrange
        string token = CreateTokenWithoutRole(123L);

        // Act
        string? role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.Null);
    }

    [Test]
    public void ExtractRole_WithInvalidToken_ReturnsNull() {
        // Arrange
        string token = "invalid.token";

        // Act
        string? role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.Null);
    }

    #endregion

    #region IsAdmin Tests

    [Test]
    public void IsAdmin_WithAdminRole_ReturnsTrue() {
        // Arrange
        string token = CreateToken(1L, "admin");

        // Act
        bool isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.True);
    }

    [Test]
    public void IsAdmin_WithAdminRoleUppercase_ReturnsTrue() {
        // Arrange
        string token = CreateToken(1L, "ADMIN");

        // Act
        bool isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.True);
    }

    [Test]
    public void IsAdmin_WithClientRole_ReturnsFalse() {
        // Arrange
        string token = CreateToken(123L, "cliente");

        // Act
        bool isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void IsAdmin_WithMissingRole_ReturnsFalse() {
        // Arrange
        string token = CreateTokenWithoutRole(123L);

        // Act
        bool isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void IsAdmin_WithInvalidToken_ReturnsFalse() {
        // Arrange
        string token = "invalid.token";

        // Act
        bool isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.False);
    }

    #endregion

    #region ExtractUserInfo Tests

    [Test]
    public void ExtractUserInfo_WithValidToken_ReturnsAllInfo() {
        // Arrange
        string token = CreateToken(123L, "admin");

        // Act
        (long? userId, bool isAdmin, string? role) = _extractor.ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.EqualTo(123L));
        Assert.That(isAdmin, Is.True);
        Assert.That(role, Is.EqualTo("admin"));
    }

    [Test]
    public void ExtractUserInfo_WithInvalidToken_ReturnsNullUserId() {
        // Arrange
        string token = "invalid.token";

        // Act
        (long? userId, bool isAdmin, string? role) = _extractor.ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    [Test]
    public void ExtractUserInfo_WithMissingRole_ReturnsCorrectInfo() {
        // Arrange
        string token = CreateTokenWithoutRole(456L);

        // Act
        (long? userId, bool isAdmin, string? role) = _extractor.ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.EqualTo(456L));
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    #endregion

    #region ExtractClaims Tests

    [Test]
    public void ExtractClaims_WithValidToken_ReturnsClaimsPrincipal() {
        // Arrange
        string token = CreateToken(123L, "admin", "test@example.com");

        // Act
        ClaimsPrincipal? claims = _extractor.ExtractClaims(token);

        // Assert
        Assert.That(claims, Is.Not.Null);
        ClaimsPrincipal c = claims!; // no-null garantizado por la asercion previa (CS8604)
        Assert.That(c.FindFirstValue(ClaimTypes.NameIdentifier), Is.EqualTo("123"));
        Assert.That(c.FindFirstValue(ClaimTypes.Role), Is.EqualTo("admin"));
        Assert.That(c.FindFirstValue(ClaimTypes.Email), Is.EqualTo("test@example.com"));
    }

    [Test]
    public void ExtractClaims_WithInvalidToken_ReturnsNull() {
        // Arrange
        string token = "invalid.token";

        // Act
        ClaimsPrincipal? claims = _extractor.ExtractClaims(token);

        // Assert
        Assert.That(claims, Is.Null);
    }

    [Test]
    public void ExtractClaims_WithEmptyToken_ReturnsNull() {
        // Arrange
        string token = "";

        // Act
        ClaimsPrincipal? claims = _extractor.ExtractClaims(token);

        // Assert
        Assert.That(claims, Is.Null);
    }

    #endregion

    #region ExtractEmail Tests

    [Test]
    public void ExtractEmail_WithValidToken_ReturnsEmail() {
        // Arrange
        string token = CreateToken(123L, "cliente", "user@example.com");

        // Act
        string? email = _extractor.ExtractEmail(token);

        // Assert
        Assert.That(email, Is.EqualTo("user@example.com"));
    }

    [Test]
    public void ExtractEmail_WithMissingEmail_ReturnsNull() {
        // Arrange
        string token = CreateToken(123L, "cliente");

        // Act
        string? email = _extractor.ExtractEmail(token);

        // Assert
        Assert.That(email, Is.Null);
    }

    [Test]
    public void ExtractEmail_WithInvalidToken_ReturnsNull() {
        // Arrange
        string token = "invalid.token";

        // Act
        string? email = _extractor.ExtractEmail(token);

        // Assert
        Assert.That(email, Is.Null);
    }

    #endregion

    #region IsValidTokenFormat Tests

    [Test]
    public void IsValidTokenFormat_WithValidToken_ReturnsTrue() {
        // Arrange
        string token = CreateToken(123L, "cliente");

        // Act
        bool isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void IsValidTokenFormat_WithInvalidToken_ReturnsFalse() {
        // Arrange
        string token = "invalid.token";

        // Act
        bool isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithEmptyToken_ReturnsFalse() {
        // Arrange
        string token = "";

        // Act
        bool isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithNullToken_ReturnsFalse() {
        // Act
        bool isValid = _extractor.IsValidTokenFormat(null!);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithOnlyTwoParts_ReturnsFalse() {
        // Arrange
        string token = "header.payload"; // Solo 2 partes

        // Act
        bool isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithFourParts_ReturnsFalse() {
        // Arrange
        string token = "header.payload.signature.extra"; // 4 partes

        // Act
        bool isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    #endregion

    #region Helpers

    private static string CreateToken(long userId, string role, string? email = null) {
        var handler = new JwtSecurityTokenHandler();
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };

        if (email != null) {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1)
        };

        SecurityToken token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private static string CreateTokenWithoutRole(long userId) {
        var handler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1)
        };

        SecurityToken token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    #endregion
}
