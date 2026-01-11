using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;
using TiendaApi.Services.Auth;

namespace TiendaApi.Tests;

/// <summary>
/// Unit tests for AuthService using Result Pattern
/// Tests the Railway Oriented Programming flow for SignUp and SignIn
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

    #region SignUp Tests

    [Test]
    public async Task SignUpAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
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

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Username.Should().Be("newuser");
        result.Value.User.Email.Should().Be("newuser@test.com");
        result.Value.User.Role.Should().Be(UserRoles.USER);
    }

    [Test]
    public async Task SignUpAsync_WithEmptyUsername_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "",
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("Username");
    }

    [Test]
    public async Task SignUpAsync_WithShortUsername_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "ab",
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("at least 3 characters");
    }

    [Test]
    public async Task SignUpAsync_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "invalidemail",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("email");
    }

    [Test]
    public async Task SignUpAsync_WithShortPassword_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "12345"
        };

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("at least 6 characters");
    }

    [Test]
    public async Task SignUpAsync_WithDuplicateUsername_ShouldReturnConflict()
    {
        // Arrange
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

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("Username already exists");
    }

    [Test]
    public async Task SignUpAsync_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
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

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("Email already exists");
    }

    #endregion

    #region SignIn Tests

    [Test]
    public async Task SignInAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
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

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("test-jwt-token");
        result.Value.User.Username.Should().Be("testuser");
    }

    [Test]
    public async Task SignInAsync_WithEmptyUsername_ShouldReturnValidationError()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("Username");
    }

    [Test]
    public async Task SignInAsync_WithEmptyPassword_ShouldReturnValidationError()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = ""
        };

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("Password");
    }

    [Test]
    public async Task SignInAsync_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "nonexistent",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("Invalid username or password");
    }

    [Test]
    public async Task SignInAsync_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
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

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("Invalid username or password");
    }

    #endregion
}
