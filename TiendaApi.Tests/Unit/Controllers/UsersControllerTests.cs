using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Tests.Unit.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);
    }

    #region GetAll Tests

    [Test]
    public async Task GetAll_ConUsuariosExistentes_RetornaOkConListaPaginada()
    {
        var usuarios = new List<UserDto>
        {
            new UserDto(1, "user1", "user1@test.com", "", "USER", DateTime.UtcNow),
            new UserDto(2, "user2", "user2@test.com", "", "USER", DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<UserDto>
        {
            Items = usuarios,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<UserDto>, DomainError>(pagedResult));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<PagedResult<UserDto>>().Subject;
        returnedUsers.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAll_SinUsuarios_RetornaOkConListaVacia()
    {
        var pagedResult = new PagedResult<UserDto>
        {
            Items = new List<UserDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<UserDto>, DomainError>(pagedResult));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<PagedResult<UserDto>>().Subject;
        returnedUsers.Items.Should().BeEmpty();
    }

    [Test]
    public async Task GetAll_ConFiltroUsername_RetornaListaFiltrada()
    {
        var usuarios = new List<UserDto>
        {
            new UserDto(1, "admin", "admin@test.com", "", "ADMIN", DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<UserDto>
        {
            Items = usuarios,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<UserDto>, DomainError>(pagedResult));

        var result = await _controller.GetAll(username: "admin");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion

    #region GetById Tests

    [Test]
    public async Task GetById_ConIdExistente_RetornaOkConUsuario()
    {
        var usuario = new UserDto(1, "testuser", "test@test.com", "", "USER", DateTime.UtcNow);

        _mockUserService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuario));

        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsuario = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Id.Should().Be(1);
        returnedUsuario.Username.Should().Be("testuser");
    }

    [Test]
    public async Task GetById_ConIdNoExistente_RetornaNotFound()
    {
        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.GetById(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Test]
    public async Task Create_ConDtoValido_RetornaCreatedConUsuario()
    {
        var registerDto = new RegisterDto { Username = "nuevouser", Email = "nuevo@test.com", Password = "Password123" };
        var usuarioDto = new UserDto(1, "nuevouser", "nuevo@test.com", "", "USER", DateTime.UtcNow);

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.Create(registerDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UsersController.GetById));
        var returnedUsuario = createdResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Username.Should().Be("nuevouser");
    }

    [Test]
    public async Task Create_ConUsernameDuplicado_RetornaConflict()
    {
        var registerDto = new RegisterDto { Username = "existente", Email = "nuevo@test.com", Password = "Password123" };
        var error = new ConflictError("El nombre de usuario ya existe");

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.Create(registerDto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task Create_ConValidacionFallida_RetornaBadRequest()
    {
        var registerDto = new RegisterDto { Username = "ab", Email = "invalido", Password = "123" };
        var error = new ValidationError("Errores de validación", new Dictionary<string, string[]>
        {
            { "Username", new[] { "El nombre de usuario debe tener al menos 3 caracteres" } }
        });

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.Create(registerDto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Update Tests

    [Test]
    public async Task Update_ConIdValido_RetornaOkConUsuarioActualizado()
    {
        var id = 1L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var usuarioDto = new UserDto(1, "testuser", "nuevo@test.com", "", "USER", DateTime.UtcNow);

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.Update(id, updateDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsuario = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Email.Should().Be("nuevo@test.com");
    }

    [Test]
    public async Task Update_ConIdNoExistente_RetornaNotFound()
    {
        var id = 999L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.Update(id, updateDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Update_ConEmailDuplicado_RetornaConflict()
    {
        var id = 1L;
        var updateDto = new UserUpdateDto { Email = "existente@test.com" };
        var error = new ConflictError("El email ya existe");

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.Update(id, updateDto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    #endregion

    #region UpdateAvatar Tests

    [Test]
    public async Task UpdateAvatar_ConUrlValida_RetornaOk()
    {
        var id = 1L;
        var avatarDto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };
        var usuarioDto = new UserDto(1, "testuser", "https://example.com/avatar.jpg", "https://example.com/avatar.jpg", "USER", DateTime.UtcNow);

        SetupUserClaims(id);

        _mockUserService.Setup(s => s.UpdateAvatarAsync(id, avatarDto.AvatarUrl))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.UpdateAvatar(id, avatarDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAvatar_ConIdNoExistente_RetornaNotFound()
    {
        var id = 999L;
        var avatarDto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };

        SetupUserClaims(id);

        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.UpdateAvatarAsync(id, avatarDto.AvatarUrl))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.UpdateAvatar(id, avatarDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_ConIdExistente_RetornaNoContent()
    {
        _mockUserService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task Delete_ConIdNoExistente_RetornaNotFound()
    {
        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.Delete(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetMyProfile Tests

    [Test]
    public async Task GetMyProfile_UsuarioAutenticado_RetornaOkConPerfil()
    {
        var userId = 1L;
        var usuarioDto = new UserDto(1, "testuser", "test@test.com", "", "USER", DateTime.UtcNow);

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.GetMyProfile();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsuario = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Id.Should().Be(1);
    }

    [Test]
    public async Task GetMyProfile_SinClaim_RetornaUnauthorized()
    {
        SetupEmptyClaims();

        var result = await _controller.GetMyProfile();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region UpdateMyProfile Tests

    [Test]
    public async Task UpdateMyProfile_ConDtoValido_RetornaOk()
    {
        var userId = 1L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var usuarioDto = new UserDto(1, "testuser", "nuevo@test.com", "", "USER", DateTime.UtcNow);

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.UpdateMyProfile(updateDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateMyProfile_ConValidacionFallida_RetornaBadRequest()
    {
        var userId = 1L;
        var updateDto = new UserUpdateDto { Email = "email-invalido" };
        var error = ValidationError.Create("El email no es válido");

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.UpdateMyProfile(updateDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteMyProfile Tests

    [Test]
    public async Task DeleteMyProfile_UsuarioAutenticado_RetornaNoContent()
    {
        var userId = 1L;

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.DeleteAsync(userId))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.DeleteMyProfile();

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeleteMyProfile_UsuarioNoExistente_RetornaNotFound()
    {
        var userId = 999L;
        var error = new NotFoundError("Usuario no encontrado");

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.DeleteAsync(userId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.DeleteMyProfile();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task GetAll_ConErrorInterno_Retorna500()
    {
        var error = new InternalError("Error de base de datos");

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Failure<PagedResult<UserDto>, DomainError>(error));

        var result = await _controller.GetAll();

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task GetById_ConErrorInterno_Retorna500()
    {
        var error = new InternalError("Error inesperado");

        _mockUserService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.GetById(1);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Authorization Attribute Tests

    [Test]
    public void GetAll_TieneAtributoAuthorizeAdmin()
    {
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetAll));
        var attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Contain("ADMIN");
    }

    [Test]
    public void GetById_TieneAtributoAuthorizeAdmin()
    {
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetById));
        var attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Contain("ADMIN");
    }

    [Test]
    public void GetMyProfile_TieneAtributoAuthorize()
    {
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetMyProfile));
        var attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true);
        attribute.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private void SetupUserClaims(long userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "USER")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupEmptyClaims()
    {
        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #endregion
}
