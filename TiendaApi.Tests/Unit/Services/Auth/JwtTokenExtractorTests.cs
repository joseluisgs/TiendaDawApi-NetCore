using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Tests.Unit.Services.Auth;

/// <summary>
/// Tests unitarios para JwtTokenExtractor.
/// Verifica la extracción de información de tokens JWT.
/// </summary>
public class JwtTokenExtractorTests
{
    private readonly Mock<ILogger<JwtTokenExtractor>> _mockLogger;
    private readonly JwtTokenExtractor _extractor;

    public JwtTokenExtractorTests()
    {
        _mockLogger = new Mock<ILogger<JwtTokenExtractor>>();
        _extractor = new JwtTokenExtractor(_mockLogger.Object);
    }

    #region ExtractUserId Tests

    [Test]
    public void ExtractUserId_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "cliente");

        // Act
        var userId = _extractor.ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.EqualTo(123L));
    }

    [Test]
    public void ExtractUserId_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var userId = _extractor.ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void ExtractUserId_WithEmptyToken_ReturnsNull()
    {
        // Arrange
        var token = "";

        // Act
        var userId = _extractor.ExtractUserId(token);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void ExtractUserId_WithNullToken_ReturnsNull()
    {
        // Act
        var userId = _extractor.ExtractUserId(null!);

        // Assert
        Assert.That(userId, Is.Null);
    }

    #endregion

    #region ExtractRole Tests

    [Test]
    public void ExtractRole_WithAdminRole_ReturnsAdmin()
    {
        // Arrange
        var token = CreateToken(userId: 1L, role: "admin");

        // Act
        var role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.EqualTo("admin"));
    }

    [Test]
    public void ExtractRole_WithClientRole_ReturnsCliente()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "cliente");

        // Act
        var role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.EqualTo("cliente"));
    }

    [Test]
    public void ExtractRole_WithMissingRole_ReturnsNull()
    {
        // Arrange
        var token = CreateTokenWithoutRole(userId: 123L);

        // Act
        var role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.Null);
    }

    [Test]
    public void ExtractRole_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var role = _extractor.ExtractRole(token);

        // Assert
        Assert.That(role, Is.Null);
    }

    #endregion

    #region IsAdmin Tests

    [Test]
    public void IsAdmin_WithAdminRole_ReturnsTrue()
    {
        // Arrange
        var token = CreateToken(userId: 1L, role: "admin");

        // Act
        var isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.True);
    }

    [Test]
    public void IsAdmin_WithAdminRoleUppercase_ReturnsTrue()
    {
        // Arrange
        var token = CreateToken(userId: 1L, role: "ADMIN");

        // Act
        var isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.True);
    }

    [Test]
    public void IsAdmin_WithClientRole_ReturnsFalse()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "cliente");

        // Act
        var isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void IsAdmin_WithMissingRole_ReturnsFalse()
    {
        // Arrange
        var token = CreateTokenWithoutRole(userId: 123L);

        // Act
        var isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.False);
    }

    [Test]
    public void IsAdmin_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var isAdmin = _extractor.IsAdmin(token);

        // Assert
        Assert.That(isAdmin, Is.False);
    }

    #endregion

    #region ExtractUserInfo Tests

    [Test]
    public void ExtractUserInfo_WithValidToken_ReturnsAllInfo()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "admin");

        // Act
        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.EqualTo(123L));
        Assert.That(isAdmin, Is.True);
        Assert.That(role, Is.EqualTo("admin"));
    }

    [Test]
    public void ExtractUserInfo_WithInvalidToken_ReturnsNullUserId()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.Null);
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    [Test]
    public void ExtractUserInfo_WithMissingRole_ReturnsCorrectInfo()
    {
        // Arrange
        var token = CreateTokenWithoutRole(userId: 456L);

        // Act
        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(token);

        // Assert
        Assert.That(userId, Is.EqualTo(456L));
        Assert.That(isAdmin, Is.False);
        Assert.That(role, Is.Null);
    }

    #endregion

    #region ExtractClaims Tests

    [Test]
    public void ExtractClaims_WithValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "admin", email: "test@example.com");

        // Act
        var claims = _extractor.ExtractClaims(token);

        // Assert
        Assert.That(claims, Is.Not.Null);
        Assert.That(claims!.FindFirstValue(ClaimTypes.NameIdentifier), Is.EqualTo("123"));
        Assert.That(claims.FindFirstValue(ClaimTypes.Role), Is.EqualTo("admin"));
        Assert.That(claims.FindFirstValue(ClaimTypes.Email), Is.EqualTo("test@example.com"));
    }

    [Test]
    public void ExtractClaims_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var claims = _extractor.ExtractClaims(token);

        // Assert
        Assert.That(claims, Is.Null);
    }

    [Test]
    public void ExtractClaims_WithEmptyToken_ReturnsNull()
    {
        // Arrange
        var token = "";

        // Act
        var claims = _extractor.ExtractClaims(token);

        // Assert
        Assert.That(claims, Is.Null);
    }

    #endregion

    #region ExtractEmail Tests

    [Test]
    public void ExtractEmail_WithValidToken_ReturnsEmail()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "cliente", email: "user@example.com");

        // Act
        var email = _extractor.ExtractEmail(token);

        // Assert
        Assert.That(email, Is.EqualTo("user@example.com"));
    }

    [Test]
    public void ExtractEmail_WithMissingEmail_ReturnsNull()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "cliente");

        // Act
        var email = _extractor.ExtractEmail(token);

        // Assert
        Assert.That(email, Is.Null);
    }

    [Test]
    public void ExtractEmail_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var email = _extractor.ExtractEmail(token);

        // Assert
        Assert.That(email, Is.Null);
    }

    #endregion

    #region IsValidTokenFormat Tests

    [Test]
    public void IsValidTokenFormat_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = CreateToken(userId: 123L, role: "cliente");

        // Act
        var isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void IsValidTokenFormat_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var token = "";

        // Act
        var isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithNullToken_ReturnsFalse()
    {
        // Act
        var isValid = _extractor.IsValidTokenFormat(null!);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithOnlyTwoParts_ReturnsFalse()
    {
        // Arrange
        var token = "header.payload"; // Solo 2 partes

        // Act
        var isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void IsValidTokenFormat_WithFourParts_ReturnsFalse()
    {
        // Arrange
        var token = "header.payload.signature.extra"; // 4 partes

        // Act
        var isValid = _extractor.IsValidTokenFormat(token);

        // Assert
        Assert.That(isValid, Is.False);
    }

    #endregion

    #region Helpers

    private static string CreateToken(long userId, string role, string? email = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        if (email != null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
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

    #endregion
}
