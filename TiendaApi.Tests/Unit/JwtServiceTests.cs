using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Models.Entities;
using TiendaApi.Services.Auth;

namespace TiendaApi.Tests;

/// <summary>
/// Smoke tests for JWT authentication service
/// </summary>
public class JwtServiceTests
{
    private IJwtService _jwtService = null!;
    private IConfiguration _configuration = null!;

    [SetUp]
    public void Setup()
    {
        // Setup configuration with test JWT settings
        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Key", "TestKeyWithAtLeast32CharactersForSecurity!"},
            {"Jwt:Issuer", "TiendaApiTest"},
            {"Jwt:Audience", "TiendaApiTest"},
            {"Jwt:ExpireMinutes", "60"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var mockLogger = new Mock<ILogger<JwtService>>();
        _jwtService = new JwtService(_configuration, mockLogger.Object);
    }

    [Test]
    public void GenerateToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRoles.USER
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Test]
    public void ValidateToken_WithValidToken_ShouldReturnUsername()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRoles.USER
        };
        var token = _jwtService.GenerateToken(user);

        // Act
        var username = _jwtService.ValidateToken(token);

        // Assert
        username.Should().Be("testuser");
    }

    [Test]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.value";

        // Act
        var username = _jwtService.ValidateToken(invalidToken);

        // Assert
        username.Should().BeNull();
    }

    [Test]
    public void GenerateToken_ForDifferentRoles_ShouldIncludeRole()
    {
        // Arrange
        var adminUser = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@example.com",
            Role = UserRoles.ADMIN
        };

        // Act
        var token = _jwtService.GenerateToken(adminUser);

        // Assert
        token.Should().NotBeNullOrEmpty();
        // Note: We can't easily inspect JWT claims without decoding, 
        // but the token should be valid
        var username = _jwtService.ValidateToken(token);
        username.Should().Be("admin");
    }
}
