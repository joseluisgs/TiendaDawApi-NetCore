using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Usuarios;
using TiendaApi.Apis.Services.Users;

namespace TiendaApi.Tests.Unit.Services.Users;

/// <summary>
/// Tests unitarios para UserService usando Result Pattern
/// Prueba operaciones CRUD, validación y manejo de errores
/// </summary>
public class UserServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<ILogger<UserService>> _mockLogger = null!;
    private IUserService _userService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _mockUserRepository.Object,
            _mockLogger.Object
        );
    }

    #region FindAllAsync Tests

    [Test]
    public async Task FindAllAsync_ConUsuarios_RetornaTodosLosUsuarios()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Username = "user1", Email = "user1@test.com", IsDeleted = false },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", IsDeleted = false },
        };

        _mockUserRepository.Setup(x => x.FindAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _userService.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAllAsync_SinUsuarios_RetornaListaVacia()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.FindAllAsync())
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _userService.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindAllAsync_FiltraUsuariosEliminados()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Username = "user1", Email = "user1@test.com", IsDeleted = false },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", IsDeleted = true }, // Deleted
        };

        _mockUserRepository.Setup(x => x.FindAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _userService.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    #endregion

    #region FindByIdAsync Tests

    [Test]
    public async Task FindByIdAsync_ConIdExistente_RetornaExito()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            IsDeleted = false
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.FindByIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Username.Should().Be("testuser");
    }

    [Test]
    public async Task FindByIdAsync_ConIdNoExistente_RetornaFalloNoEncontrado()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.FindByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.FindByIdAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("999");
    }

    [Test]
    public async Task FindByIdAsync_ConUsuarioEliminado_RetornaFalloNoEncontrado()
    {
        // Arrange
        var deletedUser = new User
        {
            Id = 1,
            Username = "deleteduser",
            IsDeleted = true
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(deletedUser);

        // Act
        var result = await _userService.FindByIdAsync(1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_ConDatosValidos_RetornaExito()
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

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Username.Should().Be("newuser");
        result.Value.Email.Should().Be("newuser@test.com");
    }

    [Test]
    public async Task CreateAsync_ConUsernameDuplicado_RetornaFalloConflicto()
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
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("nombre de usuario");
    }

    [Test]
    public async Task CreateAsync_ConEmailDuplicado_RetornaFalloConflicto()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "existing@test.com",
            Password = "Password123!"
        };

        var existingUser = new User
        {
            Id = 1,
            Username = "existinguser",
            Email = "existing@test.com"
        };

        _mockUserRepository.Setup(x => x.FindByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(x => x.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("email");
    }

    [Test]
    public async Task CreateAsync_ConPasswordInvalido_RetornaFalloValidacion()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Password = "12345" // Too short
        };

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("6 caracteres");
    }

    [Test]
    public async Task CreateAsync_ConUsernameVacio_RetornaFalloValidacion()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "",
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("nombre de usuario");
    }

    [Test]
    public async Task CreateAsync_ConUsernameCorto_RetornaFalloValidacion()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "ab", // Too short
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("3 caracteres");
    }

    [Test]
    public async Task CreateAsync_ConEmailInvalido_RetornaFalloValidacion()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "invalidemail", // Invalid format
            Password = "Password123!"
        };

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("email");
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_ConDatosValidos_RetornaExito()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "old@test.com",
            PasswordHash = "oldHash",
            IsDeleted = false
        };

        var updateDto = new UserUpdateDto
        {
            Email = "new@test.com",
            Password = "NewPassword123!"
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((User?)null);

        var updatedUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "new@test.com",
            PasswordHash = "newHash"
        };

        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _userService.UpdateAsync(1, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("new@test.com");
    }

    [Test]
    public async Task UpdateAsync_ConIdNoExistente_RetornaFalloNoEncontrado()
    {
        // Arrange
        var updateDto = new UserUpdateDto
        {
            Email = "new@test.com"
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.UpdateAsync(999, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Test]
    public async Task UpdateAsync_ConEmailDuplicado_RetornaFalloConflicto()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "old@test.com",
            IsDeleted = false
        };

        var otherUser = new User
        {
            Id = 2,
            Username = "otheruser",
            Email = "existing@test.com"
        };

        var updateDto = new UserUpdateDto
        {
            Email = "existing@test.com"
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(otherUser);

        // Act
        var result = await _userService.UpdateAsync(1, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("email");
    }

    [Test]
    public async Task UpdateAsync_ConEmailInvalido_RetornaFalloValidacion()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "old@test.com",
            IsDeleted = false
        };

        var updateDto = new UserUpdateDto
        {
            Email = "invalidemail"
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _userService.UpdateAsync(1, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("email");
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_ConIdExistente_RetornaExito()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            IsDeleted = false
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(user);

        _mockUserRepository.Setup(x => x.UpdateAsync(It.Is<User>(u => u.IsDeleted == true)))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.DeleteAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.IsDeleted == true)), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ConIdNoExistente_RetornaFalloNoEncontrado()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.FindByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.DeleteAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Test]
    public async Task DeleteAsync_ConUsuarioYaEliminado_RetornaFalloNoEncontrado()
    {
        // Arrange
        var deletedUser = new User
        {
            Id = 1,
            Username = "testuser",
            IsDeleted = true
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(deletedUser);

        // Act
        var result = await _userService.DeleteAsync(1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion
}
