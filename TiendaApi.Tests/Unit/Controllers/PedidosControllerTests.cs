using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TiendaApi.Apis.Controllers;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Services.Pedidos;

namespace TiendaApi.Tests.Unit.Controllers;

/// <summary>
/// Tests unitarios para PedidosController
/// </summary>
public class PedidosControllerTests
{
    private readonly Mock<IPedidosService> _mockService;
    private PedidosController _controller = null!;

    public PedidosControllerTests()
    {
        _mockService = new Mock<IPedidosService>();
        _controller = new PedidosController(_mockService.Object);
    }

    private void SetupUserClaims(long userId, string role = "USER")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller = new PedidosController(_mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            }
        };
    }

    #region CreatePedido Tests

    [Test]
    public async Task CreatePedido_ConDtoValido_RetornaCreated()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var pedidoDto = new PedidoDto("123", 1, new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.CreatePedido(requestDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("GetPedidoById");
    }

    [Test]
    public async Task CreatePedido_SinItems_RetornaBadRequest()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };
        var error = ValidationError.Create("El pedido debe contener al menos un artículo");

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreatePedido(requestDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreatePedido_ProductoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 999, Cantidad = 2 }
            }
        };
        var error = new NotFoundError("Producto con ID 999 no encontrado");

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreatePedido(requestDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task CreatePedido_StockInsuficiente_RetornaBadRequest()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 100 }
            }
        };
        var error = new BusinessRuleError("Stock insuficiente");

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreatePedido(requestDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreatePedido_ConflictoDeStock_RetornaConflict()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var error = new ConflictError("El producto fue adquirido por otro usuario");

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreatePedido(requestDto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task CreatePedido_SinAutenticacion_RetornaUnauthorized()
    {
        var controller = new PedidosController(_mockService.Object);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };

        var result = await controller.CreatePedido(requestDto);

        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(401);
    }

    [Test]
    public async Task CreatePedido_ErrorInterno_Retorna500()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var error = new InternalError("Error en base de datos");

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreatePedido(requestDto);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetMyPedidos Tests

    [Test]
    public async Task GetMyPedidos_ConPedidos_RetornaOk()
    {
        SetupUserClaims(1);
        var pedidos = new List<PedidoDto>
        {
            new PedidoDto("1", 1, new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow),
            new PedidoDto("2", 1, new List<PedidoItemDto>(), 200m, "PENDIENTE", null, DateTime.UtcNow)
        };

        _mockService.Setup(s => s.FindByUserIdAsync(1))
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>(pedidos));

        var result = await _controller.GetMyPedidos();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPedidos = okResult.Value.Should().BeAssignableTo<IEnumerable<PedidoDto>>().Subject;
        returnedPedidos.Should().HaveCount(2);
    }

    [Test]
    public async Task GetMyPedidos_SinPedidos_RetornaOkConListaVacia()
    {
        SetupUserClaims(1);

        _mockService.Setup(s => s.FindByUserIdAsync(1))
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>(new List<PedidoDto>()));

        var result = await _controller.GetMyPedidos();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPedidos = okResult.Value.Should().BeAssignableTo<IEnumerable<PedidoDto>>().Subject;
        returnedPedidos.Should().BeEmpty();
    }

    [Test]
    public async Task GetMyPedidos_SinAutenticacion_RetornaUnauthorized()
    {
        var controller = new PedidosController(_mockService.Object);

        var result = await controller.GetMyPedidos();

        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region GetPedidoById Tests

    [Test]
    public async Task GetPedidoById_ConIdExistente_RetornaOk()
    {
        SetupUserClaims(1);
        var pedido = new PedidoDto("123", 1, new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.FindByIdAsync("123"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetPedidoById("123");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPedido = okResult.Value.Should().BeAssignableTo<PedidoDto>().Subject;
        returnedPedido.Id.Should().Be("123");
    }

    [Test]
    public async Task GetPedidoById_PedidoDeOtroUsuario_RetornaForbid()
    {
        SetupUserClaims(2);
        var pedido = new PedidoDto("123", 1, new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.FindByIdAsync("123"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetPedidoById("123");

        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task GetPedidoById_AdminPuedeVerCualquierPedido_RetornaOk()
    {
        SetupUserClaims(2, "ADMIN");
        var pedido = new PedidoDto("123", 1, new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.FindByIdAsync("123"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetPedidoById("123");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetPedidoById_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1);
        var error = new NotFoundError("Pedido no encontrado");

        _mockService.Setup(s => s.FindByIdAsync("999"))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.GetPedidoById("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetPedidoById_ErrorInterno_Retorna500()
    {
        SetupUserClaims(1);
        var error = new InternalError("Error inesperado");

        _mockService.Setup(s => s.FindByIdAsync("123"))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.GetPedidoById("123");

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region UpdatePedidoEstado Tests

    [Test]
    public async Task UpdatePedidoEstado_ConEstadoValido_RetornaOk()
    {
        var pedidoDto = new PedidoDto("123", 0, new List<PedidoItemDto>(), 0m, "ENVIADO", null, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "ENVIADO"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "ENVIADO" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPedido = okResult.Value.Should().BeAssignableTo<PedidoDto>().Subject;
        returnedPedido.Estado.Should().Be("ENVIADO");
    }

    [Test]
    public async Task UpdatePedidoEstado_ConEstadoInvalido_RetornaBadRequest()
    {
        var error = ValidationError.Create("Estado inválido");

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "INVALIDO"))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "INVALIDO" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdatePedidoEstado_PedidoNoExistente_RetornaNotFound()
    {
        var error = new NotFoundError("Pedido no encontrado");

        _mockService.Setup(s => s.UpdateEstadoAsync("999", "ENVIADO"))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.UpdatePedidoEstado("999", new UpdateEstadoDto { Estado = "ENVIADO" });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdatePedidoEstado_ErrorInterno_Retorna500()
    {
        var error = new InternalError("Error inesperado");

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "ENVIADO"))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "ENVIADO" });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Tests Adicionales - Casos Borde

    [Test]
    public async Task CreatePedido_CantidadNegativa_RetornaBadRequest()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = -1 }
            }
        };
        var error = ValidationError.Create("La cantidad debe ser mayor a 0");

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreatePedido(requestDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreatePedido_MultiplesItems_RetornaCreated()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 3 }
            }
        };
        var pedidoDto = new PedidoDto("123", 1, new List<PedidoItemDto>(), 175m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.CreatePedido(requestDto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task UpdatePedidoEstado_TransicionesValidas_RetornaOk()
    {
        var pedidoDto = new PedidoDto("123", 0, new List<PedidoItemDto>(), 0m, "PROCESANDO", null, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "PROCESANDO"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "PROCESANDO" });

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task UpdatePedidoEstado_CancelarPedido_RetornaOk()
    {
        var pedidoDto = new PedidoDto("123", 0, new List<PedidoItemDto>(), 0m, "CANCELADO", null, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "CANCELADO"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "CANCELADO" });

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetPedidoById_IdVacio_RetornaNotFound()
    {
        SetupUserClaims(1);
        var error = new NotFoundError("Pedido no encontrado");

        _mockService.Setup(s => s.FindByIdAsync(""))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.GetPedidoById("");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetAllPedidos Tests (Admin Only)

    [Test]
    public async Task GetAllPedidos_AdminAutenticado_RetornaOk()
    {
        SetupUserClaims(1, "ADMIN");
        var pedidos = new List<PedidoDto>
        {
            new("123", 1, new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow),
            new("456", 2, new List<PedidoItemDto>(), 200m, "ENTREGADO", null, DateTime.UtcNow)
        };

        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>(pedidos));

        var result = await _controller.GetAllPedidos();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pedidos);
    }

    [Test]
    public async Task GetAllPedidos_SinPedidos_RetornaOkConListaVacia()
    {
        SetupUserClaims(1, "ADMIN");

        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>(new List<PedidoDto>()));

        var result = await _controller.GetAllPedidos();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        (okResult!.Value as IEnumerable<PedidoDto>).Should().BeEmpty();
    }

    [Test]
    public async Task GetAllPedidos_UsuarioNoAdmin_RetornaOkConError()
    {
        SetupUserClaims(1, "USER");

        var result = await _controller.GetAllPedidos();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Test]
    public async Task GetAllPedidos_ErrorInterno_Retorna500()
    {
        SetupUserClaims(1, "ADMIN");

        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Failure<IEnumerable<PedidoDto>, DomainError>(
                new InternalError("Error interno")));

        var result = await _controller.GetAllPedidos();

        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region UpdatePedido Tests

    [Test]
    public async Task UpdatePedido_UsuarioPropietario_RetornaOk()
    {
        SetupUserClaims(1, "USER");
        var updateDto = new UpdatePedidoDto { Estado = "PROCESANDO" };
        var pedidoDto = new PedidoDto("123", 1, new List<PedidoItemDto>(), 100m, "PROCESANDO", null, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateAsync("123", 1, updateDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdatePedido("123", updateDto);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pedidoDto);
    }

    [Test]
    public async Task UpdatePedido_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1, "USER");
        var updateDto = new UpdatePedidoDto();

        _mockService.Setup(s => s.UpdateAsync("999", 1, updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(
                new NotFoundError("Pedido no encontrado")));

        var result = await _controller.UpdatePedido("999", updateDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdatePedido_SinAutenticacion_RetornaUnauthorized()
    {
        var identity = new ClaimsIdentity();
        _controller = new PedidosController(_mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };

        var result = await _controller.UpdatePedido("123", new UpdatePedidoDto());

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public async Task UpdatePedido_Conflicto_RetornaBadRequest()
    {
        SetupUserClaims(1, "USER");
        var updateDto = new UpdatePedidoDto();

        _mockService.Setup(s => s.UpdateAsync("123", 1, updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(
                new BusinessRuleError("No se puede actualizar un pedido ENTREGADO")));

        var result = await _controller.UpdatePedido("123", updateDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdatePedido_ErrorInterno_Retorna500()
    {
        SetupUserClaims(1, "USER");
        var updateDto = new UpdatePedidoDto();

        _mockService.Setup(s => s.UpdateAsync("123", 1, updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(
                new InternalError("Error inesperado")));

        var result = await _controller.UpdatePedido("123", updateDto);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region DeletePedido Tests

    [Test]
    public async Task DeletePedido_UsuarioPropietario_RetornaNoContent()
    {
        SetupUserClaims(1, "USER");

        _mockService.Setup(s => s.DeleteAsync("123", 1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.DeletePedido("123");

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeletePedido_Admin_RetornaNoContent()
    {
        SetupUserClaims(1, "ADMIN");

        _mockService.Setup(s => s.DeleteAsync("123", 1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.DeletePedido("123");

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeletePedido_PedidoDeOtroUsuario_Retorna403()
    {
        SetupUserClaims(1, "USER");

        _mockService.Setup(s => s.DeleteAsync("123", 1))
            .ReturnsAsync(UnitResult.Failure<DomainError>(
                new ForbiddenError("No eres propietario de este pedido")));

        var result = await _controller.DeletePedido("123");

        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task DeletePedido_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1, "USER");

        _mockService.Setup(s => s.DeleteAsync("999", 1))
            .ReturnsAsync(UnitResult.Failure<DomainError>(
                new NotFoundError("Pedido no encontrado")));

        var result = await _controller.DeletePedido("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task DeletePedido_SinAutenticacion_RetornaUnauthorized()
    {
        var identity = new ClaimsIdentity();
        _controller = new PedidosController(_mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };

        var result = await _controller.DeletePedido("123");

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public async Task DeletePedido_ErrorInterno_Retorna500()
    {
        SetupUserClaims(1, "USER");

        _mockService.Setup(s => s.DeleteAsync("123", 1))
            .ReturnsAsync(UnitResult.Failure<DomainError>(
                new InternalError("Error al eliminar pedido")));

        var result = await _controller.DeletePedido("123");

        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}
