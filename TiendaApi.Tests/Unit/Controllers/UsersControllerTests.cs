using System.Reflection;
using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Historial;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Tests.Unit.Controllers;

public class UsersControllerTests {
    private readonly UsersController _controller;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly Mock<IUserService> _mockUserService;

    public UsersControllerTests() {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        // IHistorialService mockeado: estos tests no tocan el historial/feed (lo
        // cubren los tests unit e integration del historial).
        _controller = new UsersController(_mockUserService.Object, Mock.Of<IHistorialService>(), _mockLogger.Object);
    }

    #region GetAll Tests

    [Test]
    public async Task GetAll_ConUsuariosExistentes_RetornaOkConListaPaginada() {
        var usuarios = new List<UserDto> {
            new(1, "user1", "user1@test.com", "", "USER", DateTime.UtcNow),
            new(2, "user2", "user2@test.com", "", "USER", DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<UserDto> {
            Items = usuarios,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<UserDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<UserDto>> result = await _controller.GetAll();

        PagedResult<UserDto>? okResult = result.Value;
        PagedResult<UserDto> returnedUsers = okResult.Should().BeAssignableTo<PagedResult<UserDto>>().Subject;
        returnedUsers.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAll_SinUsuarios_RetornaOkConListaVacia() {
        var pagedResult = new PagedResult<UserDto> {
            Items = new List<UserDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<UserDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<UserDto>> result = await _controller.GetAll();

        PagedResult<UserDto>? okResult = result.Value;
        PagedResult<UserDto> returnedUsers = okResult.Should().BeAssignableTo<PagedResult<UserDto>>().Subject;
        returnedUsers.Items.Should().BeEmpty();
    }

    [Test]
    public async Task GetAll_ConFiltroUsername_RetornaListaFiltrada() {
        var usuarios = new List<UserDto> {
            new(1, "admin", "admin@test.com", "", "ADMIN", DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<UserDto> {
            Items = usuarios,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<UserDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<UserDto>> result = await _controller.GetAll("admin");

        PagedResult<UserDto>? okResult = result.Value;
        okResult.Should().NotBeNull();
    }

    #endregion

    #region GetById Tests

    [Test]
    public async Task GetById_ConIdExistente_RetornaOkConUsuario() {
        var usuario = new UserDto(1, "testuser", "test@test.com", "", "USER", DateTime.UtcNow);

        _mockUserService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuario));

        ActionResult<UserDto> result = await _controller.GetById(1);

        UserDto? okResult = result.Value;
        UserDto returnedUsuario = okResult.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Id.Should().Be(1);
        returnedUsuario.Username.Should().Be("testuser");
    }

    [Test]
    public async Task GetById_ConIdNoExistente_RetornaNotFound() {
        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Test]
    public async Task Create_ConDtoValido_RetornaCreatedConUsuario() {
        var registerDto = new RegisterDto
            { Username = "nuevouser", Email = "nuevo@test.com", Password = "Password123" };
        var usuarioDto = new UserDto(1, "nuevouser", "nuevo@test.com", "", "USER", DateTime.UtcNow);

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        ActionResult<UserDto> result = await _controller.Create(registerDto);

        CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UsersController.GetById));
        UserDto returnedUsuario = createdResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Username.Should().Be("nuevouser");
    }

    [Test]
    public async Task Create_ConUsernameDuplicado_RetornaConflict() {
        var registerDto = new RegisterDto
            { Username = "existente", Email = "nuevo@test.com", Password = "Password123" };
        var error = new ConflictError("El nombre de usuario ya existe");

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.Create(registerDto);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task Create_ConValidacionFallida_RetornaBadRequest() {
        var registerDto = new RegisterDto { Username = "ab", Email = "invalido", Password = "123" };
        var error = new ValidationError("Errores de validación", new Dictionary<string, string[]> {
            { "Username", new[] { "El nombre de usuario debe tener al menos 3 caracteres" } }
        });

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.Create(registerDto);

        BadRequestObjectResult badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Update Tests

    [Test]
    public async Task Update_ConIdValido_RetornaOkConUsuarioActualizado() {
        long id = 1L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var usuarioDto = new UserDto(1, "testuser", "nuevo@test.com", "", "USER", DateTime.UtcNow);

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        ActionResult<UserDto> result = await _controller.Update(id, updateDto);

        UserDto? okResult = result.Value;
        UserDto returnedUsuario = okResult.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Email.Should().Be("nuevo@test.com");
    }

    [Test]
    public async Task Update_ConIdNoExistente_RetornaNotFound() {
        long id = 999L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.Update(id, updateDto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Update_ConEmailDuplicado_RetornaConflict() {
        long id = 1L;
        var updateDto = new UserUpdateDto { Email = "existente@test.com" };
        var error = new ConflictError("El email ya existe");

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.Update(id, updateDto);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    #endregion

    #region UpdateAvatar Tests

    [Test]
    public async Task UpdateAvatar_ConUrlValida_RetornaOk() {
        long id = 1L;
        var avatarDto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };
        var usuarioDto = new UserDto(1, "testuser", "https://example.com/avatar.jpg", "https://example.com/avatar.jpg",
            "USER", DateTime.UtcNow);

        SetupUserClaims(id);

        _mockUserService.Setup(s => s.UpdateAvatarAsync(id, avatarDto.AvatarUrl))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        ActionResult<UserDto> result = await _controller.UpdateAvatar(id, avatarDto);

        UserDto? okResult = result.Value;
        okResult.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAvatar_ConIdNoExistente_RetornaNotFound() {
        long id = 999L;
        var avatarDto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };

        SetupUserClaims(id);

        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.UpdateAvatarAsync(id, avatarDto.AvatarUrl))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.UpdateAvatar(id, avatarDto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Test]
    public async Task Delete_ConIdExistente_RetornaNoContent() {
        _mockUserService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        ActionResult result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task Delete_ConIdNoExistente_RetornaNotFound() {
        var error = new NotFoundError("Usuario no encontrado");

        _mockUserService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        ActionResult result = await _controller.Delete(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetMyProfile Tests

    [Test]
    public async Task GetMyProfile_UsuarioAutenticado_RetornaOkConPerfil() {
        long userId = 1L;
        var usuarioDto = new UserDto(1, "testuser", "test@test.com", "", "USER", DateTime.UtcNow);

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        ActionResult<UserDto> result = await _controller.GetMyProfile();

        UserDto? okResult = result.Value;
        UserDto returnedUsuario = okResult.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Id.Should().Be(1);
    }

    [Test]
    public async Task GetMyProfile_SinClaim_RetornaUnauthorized() {
        SetupEmptyClaims();

        ActionResult<UserDto> result = await _controller.GetMyProfile();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region UpdateMyProfile Tests

    [Test]
    public async Task UpdateMyProfile_ConDtoValido_RetornaOk() {
        long userId = 1L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var usuarioDto = new UserDto(1, "testuser", "nuevo@test.com", "", "USER", DateTime.UtcNow);

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        ActionResult<UserDto> result = await _controller.UpdateMyProfile(updateDto);

        UserDto? okResult = result.Value;
        okResult.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateMyProfile_ConValidacionFallida_RetornaBadRequest() {
        long userId = 1L;
        var updateDto = new UserUpdateDto { Email = "email-invalido" };
        var error = ValidationError.Create("El email no es válido");

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.UpdateMyProfile(updateDto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteMyProfile Tests

    [Test]
    public async Task DeleteMyProfile_UsuarioAutenticado_RetornaNoContent() {
        long userId = 1L;

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.DeleteAsync(userId))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        ActionResult result = await _controller.DeleteMyProfile();

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeleteMyProfile_UsuarioNoExistente_RetornaNotFound() {
        long userId = 999L;
        var error = new NotFoundError("Usuario no encontrado");

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.DeleteAsync(userId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        ActionResult result = await _controller.DeleteMyProfile();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task GetAll_ConErrorInterno_Retorna500() {
        var error = new InternalError("Error de base de datos");

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Failure<PagedResult<UserDto>, DomainError>(error));

        ActionResult<PagedResult<UserDto>> result = await _controller.GetAll();

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task GetById_ConErrorInterno_Retorna500() {
        var error = new InternalError("Error inesperado");

        _mockUserService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        ActionResult<UserDto> result = await _controller.GetById(1);

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Authorization Attribute Tests

    [Test]
    public void GetAll_TieneAtributoAuthorizeAdmin() {
        MethodInfo? methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetAll));
        AuthorizeAttribute? attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Contain("ADMIN");
    }

    [Test]
    public void GetById_TieneAtributoAuthorizeAdmin() {
        MethodInfo? methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetById));
        AuthorizeAttribute? attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Contain("ADMIN");
    }

    [Test]
    public void GetMyProfile_TieneAtributoAuthorize() {
        MethodInfo? methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetMyProfile));
        object[] attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true);
        attribute.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private void SetupUserClaims(long userId) {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "USER")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupEmptyClaims() {
        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #endregion
}
