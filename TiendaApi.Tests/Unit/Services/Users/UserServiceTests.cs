using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Tests.Unit.Services.Users;

/// <summary>
/// Tests unitarios para UserService usando Result Pattern
/// Prueba operaciones CRUD, validación y manejo de errores
/// </summary>
public class UserServiceTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<ILogger<UserService>> _mockLogger = null!;
    private Mock<IValidator<RegisterDto>> _mockRegisterValidator = null!;
    private Mock<IValidator<UserUpdateDto>> _mockUpdateValidator = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private IUserService _userService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockRegisterValidator = new Mock<IValidator<RegisterDto>>();
        _mockUpdateValidator = new Mock<IValidator<UserUpdateDto>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockRegisterValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockUpdateValidator.Setup(v => v.ValidateAsync(It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<UserDto>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<UserDto>?)null);

        _mockCacheService.Setup(x => x.GetAsync<UserDto>(It.IsAny<string>()))
            .ReturnsAsync((UserDto?)null);

        _mockConfiguration.SetupGet(c => c["Cache:UsuarioCacheTTLMinutes"])
            .Returns("10");

        _userService = new UserService(
            _mockUserRepository.Object,
            _mockLogger.Object,
            _mockRegisterValidator.Object,
            _mockUpdateValidator.Object,
            _mockCacheService.Object,
            _mockConfiguration.Object
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

    #region FindAllPagedAsync Tests

    [Test]
    public async Task FindAllPagedAsync_ConUsuarios_RetornaPaginaCorrecta()
    {
        // Arrange
        var users = Enumerable.Range(1, 25).Select(i => new User
        {
            Id = i,
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            IsDeleted = false
        }).ToList();

        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users.Take(10).ToList(), 25));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Test]
    public async Task FindAllPagedAsync_SegundaPagina_RetornaElementosCorrectos()
    {
        // Arrange
        var users = Enumerable.Range(1, 25).Select(i => new User
        {
            Id = i,
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            IsDeleted = false
        }).ToList();

        var filter = new UserFilterDto(null, null, null, 1, 10, "id", "asc");

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users.Skip(10).Take(10).ToList(), 25));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.Page.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.Items.First().Id.Should().Be(11);
    }

    [Test]
    public async Task FindAllPagedAsync_UltimaPagina_RetornaElementosRestantes()
    {
        // Arrange
        var users = Enumerable.Range(1, 25).Select(i => new User
        {
            Id = i,
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            IsDeleted = false
        }).ToList();

        var filter = new UserFilterDto(null, null, null, 2, 10, "id", "asc");

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users.Skip(20).Take(5).ToList(), 25));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeTrue();
    }

    [Test]
    public async Task FindAllPagedAsync_SinUsuarios_RetornaListaVacia()
    {
        // Arrange
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((new List<User>(), 0));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Test]
    public async Task FindAllPagedAsync_ConFiltros_RetornaResultadosFiltrados()
    {
        // Arrange
        var filter = new UserFilterDto("test", "@test.com", false, 0, 10, "id", "asc");

        var users = new List<User>
        {
            new User { Id = 1, Username = "testuser1", Email = "user1@test.com", IsDeleted = false },
            new User { Id = 2, Username = "testuser2", Email = "user2@test.com", IsDeleted = false },
        };

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users, 2));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_ConPaginacion_RetornaPaginaCorrecta()
    {
        // Arrange
        var filter = new UserFilterDto(null, null, null, 2, 10, "id", "asc");

        var users = Enumerable.Range(1, 25).Select(i => new User
        {
            Id = i,
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            IsDeleted = false
        }).ToList();

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users.Skip(20).Take(5).ToList(), 25));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(3);
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Test]
    public async Task FindAllPagedAsync_ConOrdenacionDescendente_RetornaOrdenado()
    {
        // Arrange
        var filter = new UserFilterDto(null, null, null, 0, 10, "username", "desc");

        var users = Enumerable.Range(1, 10).Select(i => new User
        {
            Id = i,
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            IsDeleted = false
        }).ToList();

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users, 10));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
    }

    [Test]
    public async Task FindAllPagedAsync_ConPageSize100_Retorna100Elementos()
    {
        // Arrange
        var filter = new UserFilterDto(null, null, null, 0, 100, "id", "asc");

        var users = Enumerable.Range(1, 100).Select(i => new User
        {
            Id = i,
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            IsDeleted = false
        }).ToList();

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users, 100));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(100);
        result.Value.Items.Should().HaveCount(100);
    }

    [Test]
    public async Task FindAllPagedAsync_ConSortByCreatedAt_RetornaOrdenadoPorFecha()
    {
        // Arrange
        var filter = new UserFilterDto(null, null, null, 0, 10, "createdAt", "desc");

        var users = Enumerable.Range(1, 10).Select(i => new User
        {
            Id = i,
            Username = $"user{i}",
            Email = $"user{i}@test.com",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();

        _mockUserRepository.Setup(x => x.FindAllPagedAsync(filter))
            .ReturnsAsync((users, 10));

        // Act
        var result = await _userService.FindAllPagedAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
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
        result.Error.Should().BeOfType<NotFoundError>();
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
        result.Error.Should().BeOfType<NotFoundError>();
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
        result.Error.Should().BeOfType<ConflictError>();
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
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Message.Should().Contain("email");
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

        _mockRegisterValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure("Username", "El nombre de usuario es obligatorio")
            }));

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.ValidationErrors.Should().ContainKey("Username");
        validationError.ValidationErrors["Username"].Should().Contain("El nombre de usuario es obligatorio");
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

        _mockRegisterValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure("Username", "El nombre de usuario debe tener al menos 3 caracteres")
            }));

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.ValidationErrors.Should().ContainKey("Username");
        validationError.ValidationErrors["Username"].Should().ContainMatch("*al menos 3 caracteres*");
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

        _mockRegisterValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure("Email", "Debe ser un correo electrónico válido")
            }));

        // Act
        var result = await _userService.CreateAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.ValidationErrors.Should().ContainKey("Email");
        validationError.ValidationErrors["Email"].Should().Contain("Debe ser un correo electrónico válido");
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
        result.Error.Should().BeOfType<NotFoundError>();
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
        result.Error.Should().BeOfType<ConflictError>();
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

        _mockUpdateValidator.Setup(v => v.ValidateAsync(It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure("Email", "Debe ser un correo electrónico válido")
            }));

        // Act
        var result = await _userService.UpdateAsync(1, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.ValidationErrors.Should().ContainKey("Email");
        validationError.ValidationErrors["Email"].Should().Contain("Debe ser un correo electrónico válido");
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
        result.Error.Should().BeOfType<NotFoundError>();
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
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion
}
