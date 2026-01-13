using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Controllers;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Services.Users;

namespace TiendaApi.Tests.Unit.Controllers;

/// <summary>
/// Tests unitarios para UsersController.
/// Prueba operaciones CRUD de usuarios y gestión de pedidos del usuario autenticado.
/// </summary>
public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IPedidosService> _mockPedidosService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockPedidosService = new Mock<IPedidosService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_mockUserService.Object, _mockPedidosService.Object, _mockLogger.Object);
    }

    #region GetAll Tests

    /// <summary>
    /// Dado que existen usuarios, cuando se obtienen todos paginados, entonces retorna 200 OK con lista paginada.
    /// </summary>
    [Test]
    public async Task GetAll_ConUsuariosExistentes_RetornaOkConListaPaginada()
    {
        var usuarios = new List<UserDto>
        {
            new UserDto { Id = 1, Username = "user1", Email = "user1@test.com" },
            new UserDto { Id = 2, Username = "user2", Email = "user2@test.com" }
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

    /// <summary>
    /// Dado que no existen usuarios, cuando se obtienen todos, entonces retorna 200 OK con lista vacía.
    /// </summary>
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

    /// <summary>
    /// Dado un filtro por username, cuando se obtienen usuarios, entonces retorna solo los que coinciden.
    /// </summary>
    [Test]
    public async Task GetAll_ConFiltroUsername_RetornaListaFiltrada()
    {
        var usuarios = new List<UserDto>
        {
            new UserDto { Id = 1, Username = "admin", Email = "admin@test.com" }
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

    /// <summary>
    /// Dado que existe un usuario, cuando se obtiene por ID, entonces retorna 200 OK con el usuario.
    /// </summary>
    [Test]
    public async Task GetById_ConIdExistente_RetornaOkConUsuario()
    {
        var usuario = new UserDto { Id = 1, Username = "testuser", Email = "test@test.com" };

        _mockUserService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuario));

        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsuario = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Id.Should().Be(1);
        returnedUsuario.Username.Should().Be("testuser");
    }

    /// <summary>
    /// Dado que no existe un usuario, cuando se obtiene por ID, entonces retorna 404 Not Found.
    /// </summary>
    [Test]
    public async Task GetById_ConIdNoExistente_RetornaNotFound()
    {
        var error = DomainError.NotFound("Usuario no encontrado");

        _mockUserService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.GetById(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Dado un DTO válido, cuando se crea un usuario, entonces retorna 201 Created con el usuario.
    /// </summary>
    [Test]
    public async Task Create_ConDtoValido_RetornaCreatedConUsuario()
    {
        var registerDto = new RegisterDto { Username = "nuevouser", Email = "nuevo@test.com", Password = "Password123" };
        var usuarioDto = new UserDto { Id = 1, Username = "nuevouser", Email = "nuevo@test.com" };

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.Create(registerDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(UsersController.GetById));
        var returnedUsuario = createdResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Username.Should().Be("nuevouser");
    }

    /// <summary>
    /// Dado un DTO con username duplicado, cuando se crea un usuario, entonces retorna 409 Conflict.
    /// </summary>
    [Test]
    public async Task Create_ConUsernameDuplicado_RetornaConflict()
    {
        var registerDto = new RegisterDto { Username = "existente", Email = "nuevo@test.com", Password = "Password123" };
        var error = DomainError.Conflict("El nombre de usuario ya existe");

        _mockUserService.Setup(s => s.CreateAsync(registerDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.Create(registerDto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Dado un DTO con validación fallida, cuando se crea un usuario, entonces retorna 400 Bad Request.
    /// </summary>
    [Test]
    public async Task Create_ConValidacionFallida_RetornaBadRequest()
    {
        var registerDto = new RegisterDto { Username = "ab", Email = "invalido", Password = "123" };
        var error = DomainError.Validation("Errores de validación", new Dictionary<string, string[]>
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

    /// <summary>
    /// Dado un ID válido y DTO válido, cuando se actualiza, entonces retorna 200 OK con el usuario actualizado.
    /// </summary>
    [Test]
    public async Task Update_ConIdValido_RetornaOkConUsuarioActualizado()
    {
        var id = 1L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var usuarioDto = new UserDto { Id = 1, Username = "testuser", Email = "nuevo@test.com" };

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.Update(id, updateDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsuario = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Email.Should().Be("nuevo@test.com");
    }

    /// <summary>
    /// Dado un ID no existente, cuando se actualiza, entonces retorna 404 Not Found.
    /// </summary>
    [Test]
    public async Task Update_ConIdNoExistente_RetornaNotFound()
    {
        var id = 999L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var error = DomainError.NotFound("Usuario no encontrado");

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.Update(id, updateDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Dado un DTO con email duplicado, cuando se actualiza, entonces retorna 409 Conflict.
    /// </summary>
    [Test]
    public async Task Update_ConEmailDuplicado_RetornaConflict()
    {
        var id = 1L;
        var updateDto = new UserUpdateDto { Email = "existente@test.com" };
        var error = DomainError.Conflict("El email ya existe");

        _mockUserService.Setup(s => s.UpdateAsync(id, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.Update(id, updateDto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    #endregion

    #region UpdateAvatar Tests

    /// <summary>
    /// Dado un ID válido y URL de avatar, cuando se actualiza el avatar, entonces retorna 200 OK.
    /// </summary>
    [Test]
    public async Task UpdateAvatar_ConUrlValida_RetornaOk()
    {
        var id = 1L;
        var avatarDto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };
        var usuarioDto = new UserDto { Id = 1, Username = "testuser", Avatar = "https://example.com/avatar.jpg" };

        SetupUserClaims(id);

        _mockUserService.Setup(s => s.UpdateAvatarAsync(id, avatarDto.AvatarUrl))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.UpdateAvatar(id, avatarDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Dado un ID no existente, cuando se actualiza el avatar, entonces retorna 404 Not Found.
    /// </summary>
    [Test]
    public async Task UpdateAvatar_ConIdNoExistente_RetornaNotFound()
    {
        var id = 999L;
        var avatarDto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };

        SetupUserClaims(id);

        var error = DomainError.NotFound("Usuario no encontrado");

        _mockUserService.Setup(s => s.UpdateAvatarAsync(id, avatarDto.AvatarUrl))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.UpdateAvatar(id, avatarDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Dado un ID existente, cuando se elimina, entonces retorna 204 No Content.
    /// </summary>
    [Test]
    public async Task Delete_ConIdExistente_RetornaNoContent()
    {
        _mockUserService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    /// <summary>
    /// Dado un ID no existente, cuando se elimina, entonces retorna 404 Not Found.
    /// </summary>
    [Test]
    public async Task Delete_ConIdNoExistente_RetornaNotFound()
    {
        var error = DomainError.NotFound("Usuario no encontrado");

        _mockUserService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.Delete(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetMyProfile Tests

    /// <summary>
    /// Dado un usuario autenticado, cuando obtiene su perfil, entonces retorna 200 OK con el perfil.
    /// </summary>
    [Test]
    public async Task GetMyProfile_UsuarioAutenticado_RetornaOkConPerfil()
    {
        var userId = 1L;
        var usuarioDto = new UserDto { Id = 1, Username = "testuser", Email = "test@test.com" };

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.FindByIdAsync(userId))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.GetMyProfile();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsuario = okResult.Value.Should().BeAssignableTo<UserDto>().Subject;
        returnedUsuario.Id.Should().Be(1);
    }

    /// <summary>
    /// Dado un token sin claim de usuario, cuando obtiene su perfil, entonces retorna 401 Unauthorized.
    /// </summary>
    [Test]
    public async Task GetMyProfile_SinClaim_RetornaUnauthorized()
    {
        SetupEmptyClaims();

        var result = await _controller.GetMyProfile();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region UpdateMyProfile Tests

    /// <summary>
    /// Dado un usuario autenticado con DTO válido, cuando actualiza su perfil, entonces retorna 200 OK.
    /// </summary>
    [Test]
    public async Task UpdateMyProfile_ConDtoValido_RetornaOk()
    {
        var userId = 1L;
        var updateDto = new UserUpdateDto { Email = "nuevo@test.com" };
        var usuarioDto = new UserDto { Id = 1, Username = "testuser", Email = "nuevo@test.com" };

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuarioDto));

        var result = await _controller.UpdateMyProfile(updateDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Dado un usuario autenticado con validación fallida, cuando actualiza su perfil, entonces retorna 400 Bad Request.
    /// </summary>
    [Test]
    public async Task UpdateMyProfile_ConValidacionFallida_RetornaBadRequest()
    {
        var userId = 1L;
        var updateDto = new UserUpdateDto { Email = "email-invalido" };
        var error = DomainError.Validation("El email no es válido");

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.UpdateAsync(userId, updateDto))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.UpdateMyProfile(updateDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region DeleteMyProfile Tests

    /// <summary>
    /// Dado un usuario autenticado, cuando elimina su cuenta, entonces retorna 204 No Content.
    /// </summary>
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

    /// <summary>
    /// Dado un usuario autenticado que no existe, cuando elimina su cuenta, entonces retorna 404 Not Found.
    /// </summary>
    [Test]
    public async Task DeleteMyProfile_UsuarioNoExistente_RetornaNotFound()
    {
        var userId = 999L;
        var error = DomainError.NotFound("Usuario no encontrado");

        SetupUserClaims(userId);

        _mockUserService.Setup(s => s.DeleteAsync(userId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.DeleteMyProfile();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetMyPedidos Tests

    /// <summary>
    /// Dado un usuario autenticado, cuando obtiene sus pedidos paginados, entonces retorna 200 OK.
    /// </summary>
    [Test]
    public async Task GetMyPedidos_UsuarioAutenticado_RetornaOkConPedidos()
    {
        var userId = 1L;
        var pedidos = new List<PedidoDto>
        {
            new PedidoDto { Id = "ped1", UserId = 1, Total = 100 },
            new PedidoDto { Id = "ped2", UserId = 1, Total = 200 }
        };
        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = pedidos,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.FindByUserIdPagedAsync(userId, 0, 10))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult));

        var result = await _controller.GetMyPedidos();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPedidos = okResult.Value.Should().BeAssignableTo<PagedResult<PedidoDto>>().Subject;
        returnedPedidos.Items.Should().HaveCount(2);
    }

    /// <summary>
    /// Dado un usuario autenticado sin pedidos, cuando obtiene sus pedidos, entonces retorna lista vacía.
    /// </summary>
    [Test]
    public async Task GetMyPedidos_SinPedidos_RetornaOkConListaVacia()
    {
        var userId = 1L;
        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = new List<PedidoDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.FindByUserIdPagedAsync(userId, 0, 10))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult));

        var result = await _controller.GetMyPedidos();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPedidos = okResult.Value.Should().BeAssignableTo<PagedResult<PedidoDto>>().Subject;
        returnedPedidos.Items.Should().BeEmpty();
    }

    #endregion

    #region CreateMyPedido Tests

    /// <summary>
    /// Dado un usuario autenticado con DTO válido, cuando crea un pedido, entonces retorna 201 Created.
    /// </summary>
    [Test]
    public async Task CreateMyPedido_ConDtoValido_RetornaCreated()
    {
        var userId = 1L;
        var pedidoDto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };
        var pedidoResult = new PedidoDto { Id = "new-pedido", UserId = userId, Total = 0 };

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.CreateAsync(userId, pedidoDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoResult));

        var result = await _controller.CreateMyPedido(pedidoDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ControllerName.Should().Be("Pedidos");
    }

    /// <summary>
    /// Dado un usuario autenticado con producto no encontrado, cuando crea un pedido, entonces retorna 404 Not Found.
    /// </summary>
    [Test]
    public async Task CreateMyPedido_ProductoNoExistente_RetornaNotFound()
    {
        var userId = 1L;
        var pedidoDto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };
        var error = DomainError.NotFound("Producto con ID 999 no encontrado");

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.CreateAsync(userId, pedidoDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreateMyPedido(pedidoDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Dado un usuario autenticado con validación fallida, cuando crea un pedido, entonces retorna 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateMyPedido_ValidacionFallida_RetornaBadRequest()
    {
        var userId = 1L;
        var pedidoDto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };
        var error = DomainError.Validation("El pedido debe tener al menos un producto");

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.CreateAsync(userId, pedidoDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreateMyPedido(pedidoDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateMyPedido Tests

    /// <summary>
    /// Dado un usuario autenticado con DTO válido, cuando actualiza un pedido, entonces retorna 200 OK.
    /// </summary>
    [Test]
    public async Task UpdateMyPedido_ConDtoValido_RetornaOk()
    {
        var userId = 1L;
        var pedidoId = "pedido-123";
        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva dirección" };
        var pedidoDto = new PedidoDto { Id = pedidoId, UserId = userId, DireccionEnvio = "Nueva dirección" };

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.UpdateAsync(pedidoId, userId, updateDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdateMyPedido(pedidoId, updateDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Dado un usuario que intenta actualizar pedido de otro usuario, entonces retorna 403 Forbidden.
    /// </summary>
    [Test]
    public async Task UpdateMyPedido_PedidoDeOtroUsuario_RetornaForbidden()
    {
        var userId = 1L;
        var pedidoId = "pedido-123";
        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva dirección" };
        var error = DomainError.Forbidden("No puedes actualizar un pedido que no es tuyo");

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.UpdateAsync(pedidoId, userId, updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.UpdateMyPedido(pedidoId, updateDto);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region DeleteMyPedido Tests

    /// <summary>
    /// Dado un usuario autenticado, cuando elimina su pedido, entonces retorna 204 No Content.
    /// </summary>
    [Test]
    public async Task DeleteMyPedido_PedidoExistente_RetornaNoContent()
    {
        var userId = 1L;
        var pedidoId = "pedido-123";

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.DeleteAsync(pedidoId, userId))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.DeleteMyPedido(pedidoId);

        result.Should().BeOfType<NoContentResult>();
    }

    /// <summary>
    /// Dado un usuario que intenta eliminar pedido de otro usuario, entonces retorna 403 Forbidden.
    /// </summary>
    [Test]
    public async Task DeleteMyPedido_PedidoDeOtroUsuario_RetornaForbidden()
    {
        var userId = 1L;
        var pedidoId = "pedido-123";
        var error = DomainError.Forbidden("No puedes eliminar un pedido que no es tuyo");

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.DeleteAsync(pedidoId, userId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.DeleteMyPedido(pedidoId);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403);
    }

    /// <summary>
    /// Dado un pedido no existente, cuando elimina, entonces retorna 404 Not Found.
    /// </summary>
    [Test]
    public async Task DeleteMyPedido_PedidoNoExistente_RetornaNotFound()
    {
        var userId = 1L;
        var pedidoId = "pedido-inexistente";
        var error = DomainError.NotFound("Pedido no encontrado");

        SetupUserClaims(userId);

        _mockPedidosService.Setup(s => s.DeleteAsync(pedidoId, userId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.DeleteMyPedido(pedidoId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Dado un error interno en GetAll, cuando se obtienen usuarios, entonces retorna 500.
    /// </summary>
    [Test]
    public async Task GetAll_ConErrorInterno_Retorna500()
    {
        var error = DomainError.Internal("Error de base de datos");

        _mockUserService.Setup(s => s.FindAllPagedAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(Result.Failure<PagedResult<UserDto>, DomainError>(error));

        var result = await _controller.GetAll();

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Dado un error interno en GetById, cuando se obtiene usuario, entonces retorna 500.
    /// </summary>
    [Test]
    public async Task GetById_ConErrorInterno_Retorna500()
    {
        var error = DomainError.Internal("Error inesperado");

        _mockUserService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(error));

        var result = await _controller.GetById(1);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Authorization Attribute Tests

    /// <summary>
    /// Verifica que GetAll tenga el atributo Authorize con rol ADMIN.
    /// </summary>
    [Test]
    public void GetAll_TieneAtributoAuthorizeAdmin()
    {
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetAll));
        var attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Contain("ADMIN");
    }

    /// <summary>
    /// Verifica que GetById tenga el atributo Authorize con rol ADMIN.
    /// </summary>
    [Test]
    public void GetById_TieneAtributoAuthorizeAdmin()
    {
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetById));
        var attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Contain("ADMIN");
    }

    /// <summary>
    /// Verifica que GetMyProfile tenga el atributo Authorize.
    /// </summary>
    [Test]
    public void GetMyProfile_TieneAtributoAuthorize()
    {
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetMyProfile));
        var attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true);
        attribute.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifica que GetMyPedidos tenga el atributo Authorize.
    /// </summary>
    [Test]
    public void GetMyPedidos_TieneAtributoAuthorize()
    {
        var methodInfo = typeof(UsersController).GetMethod(nameof(UsersController.GetMyPedidos));
        var attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true);
        attribute.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Configura los claims del usuario para simular autenticación.
    /// </summary>
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

    /// <summary>
    /// Configura claims vacíos para simular usuario no autenticado.
    /// </summary>
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
