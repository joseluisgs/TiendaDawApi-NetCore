using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Errors;
using TiendaApi.Models;
using TiendaApi.Repositories.Usuarios;
using TiendaApi.Services.Auth;

namespace TiendaApi.Tests.Unit.Services.Auth;

/// <summary>
/// Tests unitarios para AuthService usando Patrón Result.
/// </summary>
public class AuthServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IJwtService> _mockJwtService = null!;
    private Mock<ILogger<AuthService>> _mockLogger = null!;
    private IAuthService _authService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        
        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _mockLogger.Object
        );
    }

    #region Tests SignUp

    [Test]
    public async Task SignUpAsync_ConDatosValidos_DebeRetornarExito()
    {
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        
        _mockUserRepository.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var savedUser = new User
        {
            Id = 1,
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = "hashedpassword",
            Role = UserRoles.USER,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(x => x.SaveAsync(It.IsAny<User>()))
            .ReturnsAsync(savedUser);

        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-jwt-token");

        var result = await _authService.SignUpAsync(registerDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Username.Should().Be("newuser");
        result.Value.User.Email.Should().Be("newuser@test.com");
        result.Value.User.Role.Should().Be(UserRoles.USER);
    }

    [Test]
    public async Task SignUpAsync_ConUsernameVacio_DebeRetornarErrorValidacion()
    {
        var registerDto = new RegisterDto
        {
            Username = "",
            Email = "test@test.com",
            Password = "Password123!"
        };

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("nombre de usuario");
    }

    [Test]
    public async Task SignUpAsync_ConUsernameCorto_DebeRetornarErrorValidacion()
    {
        var registerDto = new RegisterDto
        {
            Username = "ab",
            Email = "test@test.com",
            Password = "Password123!"
        };

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("al menos 3 caracteres");
    }

    [Test]
    public async Task SignUpAsync_ConEmailInvalido_DebeRetornarErrorValidacion()
    {
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "invalidemail",
            Password = "Password123!"
        };

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("email");
    }

    [Test]
    public async Task SignUpAsync_ConPasswordCorto_DebeRetornarErrorValidacion()
    {
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "12345"
        };

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("al menos 6 caracteres");
    }

    [Test]
    public async Task SignUpAsync_ConUsernameDuplicado_DebeRetornarConflicto()
    {
        var registerDto = new RegisterDto
        {
            Username = "existinguser",
            Email = "new@test.com",
            Password = "Password123!"
        };

        var existingUser = new User
        {
            Id = 1,
            Username = "existinguser",
            Email = "existing@test.com"
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("existinguser"))
            .ReturnsAsync(existingUser);

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("nombre de usuario ya existe");
    }

    [Test]
    public async Task SignUpAsync_ConEmailDuplicado_DebeRetornarConflicto()
    {
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "existing@test.com",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var existingUser = new User
        {
            Id = 1,
            Username = "existinguser",
            Email = "existing@test.com"
        };

        _mockUserRepository.Setup(x => x.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(existingUser);

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("email ya existe");
    }

    #endregion

    #region Tests SignIn

    [Test]
    public async Task SignInAsync_ConCredencialesValidas_DebeRetornarExito()
    {
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", workFactor: 11);

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = passwordHash,
            Role = UserRoles.USER,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-jwt-token");

        var result = await _authService.SignInAsync(loginDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Username.Should().Be("testuser");
    }

    [Test]
    public async Task SignInAsync_ConUsernameVacio_DebeRetornarErrorValidacion()
    {
        var loginDto = new LoginDto
        {
            Username = "",
            Password = "Password123!"
        };

        var result = await _authService.SignInAsync(loginDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("nombre de usuario");
    }

    [Test]
    public async Task SignInAsync_ConPasswordVacio_DebeRetornarErrorValidacion()
    {
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = ""
        };

        var result = await _authService.SignInAsync(loginDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("contraseña");
    }

    [Test]
    public async Task SignInAsync_ConUsuarioNoExistente_DebeRetornarNoAutorizado()
    {
        var loginDto = new LoginDto
        {
            Username = "nonexistent",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        var result = await _authService.SignInAsync(loginDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("Credenciales");
    }

    [Test]
    public async Task SignInAsync_ConPasswordInvalido_DebeRetornarNoAutorizado()
    {
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "WrongPassword!"
        };

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!", workFactor: 11);

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = passwordHash,
            Role = UserRoles.USER
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        var result = await _authService.SignInAsync(loginDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("Credenciales");
    }

    #endregion
}
