using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;
using TiendaApi.Services.Users;

namespace TiendaApi.Tests;

/// <summary>
/// Unit tests for UserService using Result Pattern
/// Tests CRUD operations, validation, and error handling
/// </summary>
public class UserServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<UserService>> _mockLogger = null!;
    private IUserService _userService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<UserService>>();
        
        _userService = new UserService(
            _mockUserRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    #region FindAllAsync Tests

    [Test]
    public async Task FindAllAsync_WithUsers_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Username = "user1", Email = "user1@test.com", IsDeleted = false },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", IsDeleted = false },
        };

        var userDtos = new List<UserDto>
        {
            new UserDto { Id = 1, Username = "user1", Email = "user1@test.com" },
            new UserDto { Id = 2, Username = "user2", Email = "user2@test.com" },
        };

        _mockUserRepository.Setup(x => x.FindAllAsync())
            .ReturnsAsync(users);

        _mockMapper.Setup(x => x.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<User>>()))
            .Returns(userDtos);

        // Act
        var result = await _userService.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(userDtos);
    }

    [Test]
    public async Task FindAllAsync_WithNoUsers_ReturnsEmptyList()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.FindAllAsync())
            .ReturnsAsync(new List<User>());

        _mockMapper.Setup(x => x.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<User>>()))
            .Returns(new List<UserDto>());

        // Act
        var result = await _userService.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindAllAsync_FiltersDeletedUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Username = "user1", Email = "user1@test.com", IsDeleted = false },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", IsDeleted = true }, // Deleted
        };

        var activeUserDtos = new List<UserDto>
        {
            new UserDto { Id = 1, Username = "user1", Email = "user1@test.com" },
        };

        _mockUserRepository.Setup(x => x.FindAllAsync())
            .ReturnsAsync(users);

        _mockMapper.Setup(x => x.Map<IEnumerable<UserDto>>(It.Is<IEnumerable<User>>(u => u.Count() == 1)))
            .Returns(activeUserDtos);

        // Act
        var result = await _userService.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    #endregion

    #region FindByIdAsync Tests

    [Test]
    public async Task FindByIdAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var user = new User 
        { 
            Id = 1, 
            Username = "testuser", 
            Email = "test@test.com", 
            IsDeleted = false 
        };

        var userDto = new UserDto 
        { 
            Id = 1, 
            Username = "testuser", 
            Email = "test@test.com" 
        };

        _mockUserRepository.Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(user);

        _mockMapper.Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _userService.FindByIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Username.Should().Be("testuser");
    }

    [Test]
    public async Task FindByIdAsync_WithNonExistentId_ReturnsNotFoundFailure()
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
    public async Task FindByIdAsync_WithDeletedUser_ReturnsNotFoundFailure()
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
    public async Task CreateAsync_WithValidData_ReturnsSuccess()
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

        var userDto = new UserDto
        {
            Id = 1,
            Username = registerDto.Username,
            Email = registerDto.Email,
            Role = UserRoles.USER
        };

        _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Username.Should().Be("newuser");
        result.Value.Email.Should().Be("newuser@test.com");
    }

    [Test]
    public async Task CreateAsync_WithDuplicateUsername_ReturnsConflictFailure()
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
    public async Task CreateAsync_WithDuplicateEmail_ReturnsConflictFailure()
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
    public async Task CreateAsync_WithInvalidPassword_ReturnsValidationFailure()
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
    public async Task CreateAsync_WithEmptyUsername_ReturnsValidationFailure()
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
    public async Task CreateAsync_WithShortUsername_ReturnsValidationFailure()
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
    public async Task CreateAsync_WithInvalidEmail_ReturnsValidationFailure()
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
    public async Task UpdateAsync_WithValidData_ReturnsSuccess()
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

        var userDto = new UserDto
        {
            Id = 1,
            Username = "testuser",
            Email = "new@test.com"
        };

        _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);

        // Act
        var result = await _userService.UpdateAsync(1, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("new@test.com");
    }

    [Test]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNotFoundFailure()
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
    public async Task UpdateAsync_WithDuplicateEmail_ReturnsConflictFailure()
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
    public async Task UpdateAsync_WithInvalidEmail_ReturnsValidationFailure()
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
    public async Task DeleteAsync_WithExistingId_ReturnsSuccess()
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
    public async Task DeleteAsync_WithNonExistentId_ReturnsNotFoundFailure()
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
    public async Task DeleteAsync_WithAlreadyDeletedUser_ReturnsNotFoundFailure()
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
