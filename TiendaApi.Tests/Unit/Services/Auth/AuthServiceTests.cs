using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
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
    private Mock<IJwtService> _mockJwtService = null!;
    private Mock<ILogger<AuthService>> _mockLogger = null!;
    private Mock<IValidator<RegisterDto>> _mockRegisterValidator = null!;
    private Mock<IValidator<LoginDto>> _mockLoginValidator = null!;
    private IAuthService _authService = null!;

    private void CreateService()
    {
        _authService = new AuthService(
            _mockUserRepository.Object,
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
            Username = registerDto.Username,
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
            Username = "existinguser",
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
            Username = "existinguser",
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
            Username = "testuser",
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
            Username = "testuser",
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
}
