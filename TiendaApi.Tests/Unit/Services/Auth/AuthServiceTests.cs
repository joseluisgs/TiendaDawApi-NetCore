using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Auth;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Auth;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Unit.Services.Auth;

/// <summary>
/// Tests unitarios para AuthService usando Patrón Result.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Service")]
[Category("Auth")]
public class AuthServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IUserLoginRepository> _mockUserLoginRepository = null!;
    private Mock<IJwtService> _mockJwtService = null!;
    private Mock<ILogger<AuthService>> _mockLogger = null!;
    private Mock<IValidator<RegisterDto>> _mockRegisterValidator = null!;
    private Mock<IValidator<LoginDto>> _mockLoginValidator = null!;
    private IAuthService _authService = null!;

    private void CreateService()
    {
        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockUserLoginRepository.Object,
            _mockJwtService.Object,
            _mockLogger.Object,
            _mockRegisterValidator.Object,
            _mockLoginValidator.Object
        );
    }

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserLoginRepository = new Mock<IUserLoginRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockRegisterValidator = new Mock<IValidator<RegisterDto>>();
        _mockLoginValidator = new Mock<IValidator<LoginDto>>();

        // Configuración por defecto: validación pasa
        _mockRegisterValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockLoginValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Mock de JWT siempre retorna un token
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-jwt-token");

        CreateService();
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
            UserName = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = "hashedpassword",
            Role = UserRoles.USER,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(x => x.SaveAsync(It.IsAny<User>()))
            .ReturnsAsync(savedUser);

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

        // Configurar validator para que falle
        _mockRegisterValidator.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Username", "El nombre de usuario es obligatorio")
            }));

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Username");
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

        _mockRegisterValidator.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Username", "El nombre de usuario debe tener al menos 3 caracteres")
            }));

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Username");
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

        _mockRegisterValidator.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Email", "Debe ser un correo electrónico válido")
            }));

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Email");
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

        _mockRegisterValidator.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Password", "La contraseña debe tener al menos 6 caracteres")
            }));

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Password");
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
            UserName = "existinguser",
            Email = "existing@test.com"
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("existinguser"))
            .ReturnsAsync(existingUser);

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Message.Should().Contain("Ya existe un nombre de usuario con el valor");
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
            UserName = "existinguser",
            Email = "existing@test.com"
        };

        _mockUserRepository.Setup(x => x.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(existingUser);

        var result = await _authService.SignUpAsync(registerDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Message.Should().Contain("Ya existe un email con el valor");
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
            UserName = "testuser",
            Email = "test@test.com",
            PasswordHash = passwordHash,
            Role = UserRoles.USER,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("testuser"))
            .ReturnsAsync(user);

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

        _mockLoginValidator.Setup(v => v.ValidateAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Username", "El nombre de usuario es obligatorio")
            }));

        var result = await _authService.SignInAsync(loginDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Username");
    }

    [Test]
    public async Task SignInAsync_ConPasswordVacio_DebeRetornarErrorValidacion()
    {
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = ""
        };

        _mockLoginValidator.Setup(v => v.ValidateAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Password", "La contraseña es obligatoria")
            }));

        var result = await _authService.SignInAsync(loginDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Password");
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
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task SignInAsync_ConPasswordIncorrecto_DebeRetornarNoAutorizado()
    {
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "WrongPassword123!"
        };

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", workFactor: 11);

        var user = new User
        {
            Id = 1,
            UserName = "testuser",
            Email = "test@test.com",
            PasswordHash = passwordHash,
            Role = UserRoles.USER
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        var result = await _authService.SignInAsync(loginDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    #endregion

    #region Tests LoginExternal

    [Test]
    public async Task LoginExternalAsync_UsuarioNuevo_DebeCrearUsuarioYVincular()
    {
        var info = new ExternalLoginDto("Google", "g-key-1", "nuevo@test.com", "Nuevo", null);

        _mockUserLoginRepository.Setup(x => x.FindByProviderAsync("Google", "g-key-1"))
            .ReturnsAsync((UserLogin?)null);
        _mockUserRepository.Setup(x => x.FindByEmailAsync("nuevo@test.com"))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(x => x.SaveAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = 42; return u; });
        _mockUserLoginRepository.Setup(x => x.ExistsAsync("Google", "g-key-1"))
            .ReturnsAsync(false);

        var result = await _authService.LoginExternalAsync(info);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Email.Should().Be("nuevo@test.com");
        result.Value.User.Role.Should().Be(UserRoles.USER);

        // Usuario creado con dummy hash (no puede hacer login con password)
        _mockUserRepository.Verify(x => x.SaveAsync(
            It.Is<User>(u => u.PasswordHash == "OAUTH-Google")), Times.Once);

        // Vinculación guardada
        _mockUserLoginRepository.Verify(x => x.SaveAsync(
            It.Is<UserLogin>(ul => ul.UserId == 42 && ul.LoginProvider == "Google"
                && ul.ProviderKey == "g-key-1")), Times.Once);
    }

    [Test]
    public async Task LoginExternalAsync_ExistentePorProvider_DebeUsarEseUsuario()
    {
        var info = new ExternalLoginDto("GitHub", "gh-key-9", "user@test.com", null, null);

        var existingUser = new User
        {
            Id = 7,
            UserName = "user@test.com",
            Email = "user@test.com",
            Role = UserRoles.USER
        };

        _mockUserLoginRepository.Setup(x => x.FindByProviderAsync("GitHub", "gh-key-9"))
            .ReturnsAsync(new UserLogin { Id = 1, UserId = 7, LoginProvider = "GitHub", ProviderKey = "gh-key-9" });
        _mockUserRepository.Setup(x => x.FindByIdAsync(7))
            .ReturnsAsync(existingUser);

        var result = await _authService.LoginExternalAsync(info);

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Username.Should().Be("user@test.com");
        // No debe crear usuario ni vincular otra vez
        _mockUserRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Never);
        _mockUserLoginRepository.Verify(x => x.SaveAsync(It.IsAny<UserLogin>()), Times.Never);
    }

    [Test]
    public async Task LoginExternalAsync_ExistentePorEmail_DebeVincularSinDuplicar()
    {
        var info = new ExternalLoginDto("Google", "g-key-new", "yaexiste@test.com", null, null);

        var existingUser = new User
        {
            Id = 5,
            UserName = "yaexiste",
            Email = "yaexiste@test.com",
            Role = UserRoles.USER
        };

        _mockUserLoginRepository.Setup(x => x.FindByProviderAsync("Google", "g-key-new"))
            .ReturnsAsync((UserLogin?)null);
        _mockUserRepository.Setup(x => x.FindByEmailAsync("yaexiste@test.com"))
            .ReturnsAsync(existingUser);
        _mockUserLoginRepository.Setup(x => x.ExistsAsync("Google", "g-key-new"))
            .ReturnsAsync(false);

        var result = await _authService.LoginExternalAsync(info);

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Username.Should().Be("yaexiste");
        // No crea usuario nuevo (ya existe por email), solo vincula
        _mockUserRepository.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Never);
        _mockUserLoginRepository.Verify(x => x.SaveAsync(
            It.Is<UserLogin>(ul => ul.UserId == 5)), Times.Once);
    }

    [Test]
    public async Task LoginExternalAsync_SinEmail_DebeRetornarBusinessRuleError()
    {
        var info = new ExternalLoginDto("GitHub", "gh-key-8", null, "Solo nombre", null);

        var result = await _authService.LoginExternalAsync(info);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
        result.Error.Message.Should().Contain("email");
    }

    #endregion
}
