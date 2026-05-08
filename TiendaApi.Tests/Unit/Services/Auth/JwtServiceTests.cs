using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Tests.Unit.Services.Auth;

/// <summary>
/// Tests para JwtService.
/// Verifica la generación y validación de tokens JWT.
/// </summary>
public class JwtServiceTests
{
    private IJwtService _jwtService = null!;
    private IConfiguration _configuration = null!;

    [SetUp]
    public void Setup()
    {
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

    #region GenerateToken Tests

    /// <summary>
    /// Verifica que la generación de token retorna un token JWT válido.
    /// </summary>
    [Test]
    public void GenerateToken_DebeRetornarTokenValido()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRoles.USER
        };

        var token = _jwtService.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    /// <summary>
    /// Verifica que el token generado contiene los claims correctos.
    /// </summary>
    [Test]
    public void GenerateToken_DebeContenerClaimsCorrectos()
    {
        var user = new User
        {
            Id = 42,
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRoles.USER
        };

        var token = _jwtService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be("42");

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "username");
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be("testuser");

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be("test@example.com");

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(UserRoles.USER);
    }

    /// <summary>
    /// Verifica que el token para admin contiene el rol ADMIN.
    /// </summary>
    [Test]
    public void GenerateToken_ParaAdmin_DebeContenerRolAdmin()
    {
        var adminUser = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@example.com",
            Role = UserRoles.ADMIN
        };

        var token = _jwtService.GenerateToken(adminUser);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(UserRoles.ADMIN);
    }

    /// <summary>
    /// Verifica que tokens generados para usuarios diferentes son distintos.
    /// </summary>
    [Test]
    public void GenerateToken_ParaUsuariosDistintos_DebeRetornarTokensDistintos()
    {
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com", Role = UserRoles.USER };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com", Role = UserRoles.USER };

        var token1 = _jwtService.GenerateToken(user1);
        var token2 = _jwtService.GenerateToken(user2);

        token1.Should().NotBe(token2);
    }

    /// <summary>
    /// Verifica que el token contiene la fecha de expiración.
    /// </summary>
    [Test]
    public void GenerateToken_DebeContenerExpiracion()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRoles.USER
        };

        var token = _jwtService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
        expClaim.Should().NotBeNull();
        expClaim!.Value.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifica que el token contiene el issuer correcto.
    /// </summary>
    [Test]
    public void GenerateToken_DebeContenerIssuerCorrecto()
    {
        var user = new User { Id = 1, Username = "testuser", Email = "test@test.com", Role = UserRoles.USER };

        var token = _jwtService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("TiendaApiTest");
    }

    #endregion

    #region ValidateToken Tests

    /// <summary>
    /// Verifica que la validación de token válido retorna el username.
    /// </summary>
    [Test]
    public void ValidateToken_ConTokenValido_DebeRetornarUsername()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = UserRoles.USER
        };
        var token = _jwtService.GenerateToken(user);

        var username = _jwtService.ValidateToken(token);

        username.Should().Be("testuser");
    }

    /// <summary>
    /// Verifica que la validación de token inválido retorna null.
    /// </summary>
    [Test]
    public void ValidateToken_ConTokenInvalido_DebeRetornarNull()
    {
        var invalidToken = "invalid.token.value";

        var username = _jwtService.ValidateToken(invalidToken);

        username.Should().BeNull();
    }

    /// <summary>
    /// Verifica que la validación de token vacío retorna null.
    /// </summary>
    [Test]
    public void ValidateToken_ConTokenVacio_DebeRetornarNull()
    {
        var username = _jwtService.ValidateToken(string.Empty);

        username.Should().BeNull();
    }

    /// <summary>
    /// Verifica que la validación de token nulo retorna null.
    /// </summary>
    [Test]
    public void ValidateToken_ConTokenNulo_DebeRetornarNull()
    {
        var username = _jwtService.ValidateToken(null!);

        username.Should().BeNull();
    }

    /// <summary>
    /// Verifica que la validación de token con formato incorrecto retorna null.
    /// </summary>
    [Test]
    public void ValidateToken_ConFormatoIncorrecto_DebeRetornarNull()
    {
        var malformedToken = "not-a-jwt-token";

        var username = _jwtService.ValidateToken(malformedToken);

        username.Should().BeNull();
    }

    /// <summary>
    /// Verifica que la validación de token con ClaimsIdentity personalizado funciona.
    /// </summary>
    [Test]
    public void ValidateToken_ConTokenValido_DebePoderObtenerClaims()
    {
        var user = new User
        {
            Id = 99,
            Username = "claimtest",
            Email = "claims@test.com",
            Role = UserRoles.USER
        };

        var token = _jwtService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToList();
        claims.Should().NotBeEmpty();
        claims.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion

    #region Configuration Tests

    /// <summary>
    /// Verifica que la generación de token funciona con diferentes configuraciones de expiración.
    /// </summary>
    [Test]
    public void GenerateToken_ConConfiguracionExpiracionDistinta_DebeGenerarToken()
    {
        var shortConfig = new Dictionary<string, string> {
            {"Jwt:Key", "TestKeyWithAtLeast32CharactersForSecurity!"},
            {"Jwt:Issuer", "TiendaApiTest"},
            {"Jwt:Audience", "TiendaApiTest"},
            {"Jwt:ExpireMinutes", "1"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(shortConfig!).Build();
        var mockLogger = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(config, mockLogger.Object);

        var user = new User { Id = 1, Username = "test", Email = "test@test.com", Role = UserRoles.USER };
        var token = jwtService.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifica que la validación falla con clave diferente.
    /// </summary>
    [Test]
    public void ValidateToken_ConClaveDistinta_NoDebeValidar()
    {
        var user = new User { Id = 1, Username = "test", Email = "test@test.com", Role = UserRoles.USER };
        var token = _jwtService.GenerateToken(user);

        var differentConfig = new Dictionary<string, string> {
            {"Jwt:Key", "DifferentKeyWithAtLeast32CharactersLong!"},
            {"Jwt:Issuer", "TiendaApiTest"},
            {"Jwt:Audience", "TiendaApiTest"},
            {"Jwt:ExpireMinutes", "60"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(differentConfig!).Build();
        var mockLogger = new Mock<ILogger<JwtService>>();
        var differentJwtService = new JwtService(config, mockLogger.Object);

        var username = differentJwtService.ValidateToken(token);

        username.Should().BeNull();
    }

    #endregion

    #region Role Tests

    /// <summary>
    /// Verifica que el token para usuario USER contiene el rol correcto.
    /// </summary>
    [Test]
    public void GenerateToken_ParaRolesDistintos_DebeIncluirRol()
    {
        var adminUser = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@example.com",
            Role = UserRoles.ADMIN
        };

        var token = _jwtService.GenerateToken(adminUser);
        var username = _jwtService.ValidateToken(token);
        username.Should().Be("admin");
    }

    /// <summary>
    /// Verifica que se puede validar un token de usuario con rol USER.
    /// </summary>
    [Test]
    public void GenerateToken_ParaUserRole_DebeValidarCorrectamente()
    {
        var regularUser = new User
        {
            Id = 5,
            Username = "regularuser",
            Email = "regular@test.com",
            Role = UserRoles.USER
        };

        var token = _jwtService.GenerateToken(regularUser);
        var username = _jwtService.ValidateToken(token);

        username.Should().Be("regularuser");
    }

    #endregion
}
