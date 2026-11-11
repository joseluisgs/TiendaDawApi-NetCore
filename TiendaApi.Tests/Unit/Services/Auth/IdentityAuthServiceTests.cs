using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Auth;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Tests.Unit.Services.Auth;

/// <summary>
///     Tests unitarios para IdentityAuthService (Auth:Mode = "Identity").
///     Espejo de <see cref="AuthServiceTests" />: mientras AuthService usa BCrypt + IUserRepository,
///     IdentityAuthService delega en UserManager/SignInManager reales de Identity (con EF InMemory)
///     para validar el flujo PBKDF2 completo. Solo se mockea IJwtService.
///     🎓 Por qué Identity real y no mocks: SignInManager tiene un constructor que Moq no puede
///     proxyar en .NET 10, y además queremos probar que la contraseña realmente se verifica
///     con el algoritmo correcto (PBKDF2), no que un mock devuelva true.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Service")]
[Category("Auth")]
public class IdentityAuthServiceTests {
    [SetUp]
    public void Setup() {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<TiendaDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddIdentity<User, IdentityRole<long>>()
            .AddEntityFrameworkStores<TiendaDbContext>()
            .AddDefaultTokenProviders();

        _mockJwtService = new Mock<IJwtService>();
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-jwt-token");
        services.AddSingleton(_mockJwtService.Object);

        services.AddScoped<IAuthService, IdentityAuthService>();

        _provider = services.BuildServiceProvider();

        // Resolvemos del root (sin validación de scopes): IdentityAuthService es scoped
        // pero vive mientras viva el provider, y así lo tenemos disponible por test.
        _authService = (IdentityAuthService)_provider.GetRequiredService<IAuthService>();
    }

    [TearDown]
    public void TearDown() {
        _provider.Dispose();
    }

    private ServiceProvider _provider = null!;
    private IdentityAuthService _authService = null!;
    private Mock<IJwtService> _mockJwtService = null!;

    private static RegisterDto NewRegisterDto(string username = "newuser", string email = "newuser@test.com",
        string password = "Password123!") {
        return new RegisterDto { Username = username, Email = email, Password = password };
    }

    [Test]
    public async Task SignUpAsync_ConDatosValidos_DebeRetornarExito() {
        Result<AuthResponseDto, DomainError> result = await _authService.SignUpAsync(NewRegisterDto());

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Username.Should().Be("newuser");
        result.Value.User.Email.Should().Be("newuser@test.com");
        result.Value.User.Role.Should().Be(UserRoles.USER);
    }

    [Test]
    public async Task SignUpAsync_ConDatosValidos_DebeGuardarHashPbkdf2NoBcrypt() {
        Result<AuthResponseDto, DomainError> result = await _authService.SignUpAsync(NewRegisterDto());

        result.IsSuccess.Should().BeTrue(result.IsFailure
            ? $"el signup falló: {result.Error.Message}"
            : "no debería fallar");

        TiendaDbContext context = _provider.GetRequiredService<TiendaDbContext>();
        User user = context.Users.First(u => u.UserName == "newuser");

        // PBKDF2 (Identity) = "AQAAAA..."; BCrypt = "$2a$..."
        // Nota: no llamamos a BCrypt.Verify porque LANZA excepción con hashes
        // que no tienen formato BCrypt, no devuelve false.
        user.PasswordHash.Should().StartWith("AQAAAA",
            "Identity genera hashes PBKDF2; jamés deben guardarse hashes BCrypt");
    }

    [Test]
    public async Task SignUpAsync_ConUsuarioDuplicado_DebeRetornarConflict() {
        await _authService.SignUpAsync(NewRegisterDto());

        Result<AuthResponseDto, DomainError> result = await _authService.SignUpAsync(NewRegisterDto());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public async Task SignUpAsync_ConPoliticaPasswordInvalida_DebeRetornarBusinessRuleError() {
        // "12345" incumple la política de Identity (sin mayúscula, sin dígito especial...)
        Result<AuthResponseDto, DomainError> result = await _authService.SignUpAsync(NewRegisterDto(password: "12345"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
    }

    [Test]
    public async Task SignInAsync_ConCredencialesValidas_DebeRetornarExito() {
        await _authService.SignUpAsync(NewRegisterDto());

        Result<AuthResponseDto, DomainError> result = await _authService.SignInAsync(new LoginDto {
            Username = "newuser",
            Password = "Password123!"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Username.Should().Be("newuser");
    }

    [Test]
    public async Task SignInAsync_ConEmailComoUsername_DebeRetornarExito() {
        // Identity permite login por nombre O por email → el servicio intenta FindByName y luego FindByEmail
        await _authService.SignUpAsync(NewRegisterDto());

        Result<AuthResponseDto, DomainError> result = await _authService.SignInAsync(new LoginDto {
            Username = "newuser@test.com",
            Password = "Password123!"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Username.Should().Be("newuser");
    }

    [Test]
    public async Task SignInAsync_ConUsuarioNoExistente_DebeRetornarUnauthorized() {
        Result<AuthResponseDto, DomainError> result = await _authService.SignInAsync(new LoginDto {
            Username = "nonexistent",
            Password = "Password123!"
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task SignInAsync_ConPasswordIncorrecto_DebeRetornarUnauthorized() {
        await _authService.SignUpAsync(NewRegisterDto());

        Result<AuthResponseDto, DomainError> result = await _authService.SignInAsync(new LoginDto {
            Username = "newuser",
            Password = "WrongPassword123!"
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task LoginExternalAsync_UsuarioNuevo_DebeCrearYRetornarExito() {
        var info = new ExternalLoginDto(
            "Google",
            "google-key-123",
            "nuevo@test.com",
            "Nuevo Usuario",
            null
        );

        Result<AuthResponseDto, DomainError> result = await _authService.LoginExternalAsync(info);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Email.Should().Be("nuevo@test.com");
        result.Value.User.Username.Should().Be("nuevo@test.com", "sin username propio, se usa el email");

        TiendaDbContext context = _provider.GetRequiredService<TiendaDbContext>();
        User user = context.Users.Single(u => u.Email == "nuevo@test.com");

        user.Role.Should().Be(UserRoles.USER);

        UserManager<User> userManager = _provider.GetRequiredService<UserManager<User>>();
        IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);
        logins.Should().ContainSingle(l => l.LoginProvider == "Google" && l.ProviderKey == "google-key-123",
            "el login externo debe quedar vinculado al usuario");
    }

    [Test]
    public async Task LoginExternalAsync_UsuarioExistente_NoDebeCrearDeNuevo() {
        var info = new ExternalLoginDto("Google", "google-key-456", "existente@test.com", null, null);

        await _authService.LoginExternalAsync(info);
        Result<AuthResponseDto, DomainError> result = await _authService.LoginExternalAsync(info);

        result.IsSuccess.Should().BeTrue();

        TiendaDbContext context = _provider.GetRequiredService<TiendaDbContext>();
        context.Users.Count(u => u.Email == "existente@test.com").Should().Be(1,
            "el segundo login externo no debe crear otro usuario");
    }

    [Test]
    public async Task LoginExternalAsync_LoginYaVinculado_NoDebeVincularOtraVez() {
        var info = new ExternalLoginDto("GitHub", "github-key-789", "vinculado@test.com", null, null);

        await _authService.LoginExternalAsync(info);
        await _authService.LoginExternalAsync(info);

        TiendaDbContext context = _provider.GetRequiredService<TiendaDbContext>();
        UserManager<User> userManager = _provider.GetRequiredService<UserManager<User>>();

        User user = context.Users.Single(u => u.Email == "vinculado@test.com");
        IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);

        logins.Should().ContainSingle(l => l.LoginProvider == "GitHub",
            "el mismo login externo no debe vincularse dos veces");
    }

    [Test]
    public async Task LoginExternalAsync_SinEmailDelProveedor_DebeRetornarBusinessRuleError() {
        // Un proveedor sin email (posible en algunos flujos) no puede mapear a usuario
        var info = new ExternalLoginDto("GitHub", "github-key-no-email", null, "Solo Nombre", null);

        Result<AuthResponseDto, DomainError> result = await _authService.LoginExternalAsync(info);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
        result.Error.Message.Should().Contain("email");
    }

    [Test]
    public async Task LoginExternalAsync_UsuarioCreadoPorOAuth_NoDebePoderLoginConPassword() {
        // 🎓 El usuario OAuth no tiene contraseña → no debe autenticarse con password
        var info = new ExternalLoginDto("Google", "google-key-pw", "sinpassword@test.com", null, null);
        await _authService.LoginExternalAsync(info);

        Result<AuthResponseDto, DomainError> result = await _authService.SignInAsync(new LoginDto {
            Username = "sinpassword@test.com",
            Password = "cualquiera123!"
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }
}
