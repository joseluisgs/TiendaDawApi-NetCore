using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Tests.Unit.Controllers;

/// <summary>
/// Tests unitarios para AuthController.
/// Verifica el funcionamiento de los endpoints de autenticación.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
    }

    #region SignUp Tests

    /// <summary>
    /// Verifica que con datos válidos se retorna Created con la respuesta de autenticación.
    /// </summary>
    [Test]
    public async Task SignUp_ConDtoValido_RetornaCreated()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "nuevousuario",
            Email = "nuevo@test.com",
            Password = "Password123"
        };

        var authResponse = new AuthResponseDto("jwt-token-123",
            new UserDto(1, "nuevousuario", "nuevo@test.com", "", "USER", DateTime.UtcNow));

        _mockAuthService.Setup(s => s.SignUpAsync(registerDto))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(authResponse));

        // Act
        var result = await _controller.SignUp(registerDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("SignUp");
        var response = createdResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        response.Token.Should().Be("jwt-token-123");
        response.User.Username.Should().Be("nuevousuario");
    }

    /// <summary>
    /// Verifica que con datos inválidos retorna BadRequest.
    /// </summary>
    [Test]
    public async Task SignUp_ConDtoInvalido_RetornaBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "",
            Email = "invalid-email",
            Password = "123"
        };

        var error = ValidationError.Create("El nombre de usuario es obligatorio");

        _mockAuthService.Setup(s => s.SignUpAsync(registerDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignUp(registerDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Verifica que con usuario duplicado retorna Conflict.
    /// </summary>
    [Test]
    public async Task SignUp_ConUsuarioDuplicado_RetornaConflict()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "existente",
            Email = "existente@test.com",
            Password = "Password123"
        };

        var error = new ConflictError("El nombre de usuario ya existe");

        _mockAuthService.Setup(s => s.SignUpAsync(registerDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignUp(registerDto);

        // Assert
        var conflictResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Verifica que con email duplicado retorna Conflict.
    /// </summary>
    [Test]
    public async Task SignUp_ConEmailDuplicado_RetornaConflict()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "nuevousuario",
            Email = "existente@test.com",
            Password = "Password123"
        };

        var error = new ConflictError("El correo electrónico ya está registrado");

        _mockAuthService.Setup(s => s.SignUpAsync(registerDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignUp(registerDto);

        // Assert
        var conflictResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Verifica que con error interno retorna StatusCode 500.
    /// </summary>
    [Test]
    public async Task SignUp_ConErrorInterno_Retorna500()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "test",
            Email = "test@test.com",
            Password = "Password123"
        };

        var error = new InternalError("Error inesperado");

        _mockAuthService.Setup(s => s.SignUpAsync(registerDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignUp(registerDto);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region SignIn Tests

    /// <summary>
    /// Verifica que con credenciales válidas retorna Ok con token JWT.
    /// </summary>
    [Test]
    public async Task SignIn_ConCredencialesValidas_RetornaOk()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "usuariovalido",
            Password = "Password123"
        };

        var authResponse = new AuthResponseDto("jwt-token-456",
            new UserDto(2, "usuariovalido", "usuario@test.com", "", "USER", DateTime.UtcNow));

        _mockAuthService.Setup(s => s.SignInAsync(loginDto))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(authResponse));

        // Act
        var result = await _controller.SignIn(loginDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        response.Token.Should().Be("jwt-token-456");
        response.User.Username.Should().Be("usuariovalido");
    }

    /// <summary>
    /// Verifica que con credenciales inválidas retorna Unauthorized.
    /// </summary>
    [Test]
    public async Task SignIn_ConCredencialesInvalidas_RetornaUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "usuario",
            Password = "password-incorrecto"
        };

        var error = new UnauthorizedError("Credenciales inválidas");

        _mockAuthService.Setup(s => s.SignInAsync(loginDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignIn(loginDto);

        // Assert
        var unauthorizedResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Verifica que con usuario no encontrado retorna Unauthorized.
    /// </summary>
    [Test]
    public async Task SignIn_ConUsuarioNoExistente_RetornaUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "noexiste",
            Password = "Password123"
        };

        var error = new UnauthorizedError("Credenciales inválidas");

        _mockAuthService.Setup(s => s.SignInAsync(loginDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignIn(loginDto);

        // Assert
        var unauthorizedResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Verifica que con validación fallida retorna BadRequest.
    /// </summary>
    [Test]
    public async Task SignIn_ConValidacionFallida_RetornaBadRequest()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "",
            Password = "Password123"
        };

        var error = ValidationError.Create("El nombre de usuario es obligatorio");

        _mockAuthService.Setup(s => s.SignInAsync(loginDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignIn(loginDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Verifica que con error interno retorna StatusCode 500.
    /// </summary>
    [Test]
    public async Task SignIn_ConErrorInterno_Retorna500()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "test",
            Password = "Password123"
        };

        var error = new InternalError("Error al procesar la solicitud");

        _mockAuthService.Setup(s => s.SignInAsync(loginDto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var result = await _controller.SignIn(loginDto);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifica que el controlador se crea correctamente con las dependencias.
    /// </summary>
    [Test]
    public void Constructor_ConDependenciasValidas_CreaControlador()
    {
        // Arrange & Act
        var controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    #endregion
}
