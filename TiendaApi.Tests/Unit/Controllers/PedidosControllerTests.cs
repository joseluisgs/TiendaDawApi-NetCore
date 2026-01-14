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
        var pedidoDto = new PedidoDto
        {
            Id = "123",
            UserId = 1,
            Total = 100
        };

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
            new() { Id = "1", UserId = 1, Total = 100 },
            new() { Id = "2", UserId = 1, Total = 200 }
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
        var pedido = new PedidoDto { Id = "123", UserId = 1, Total = 100 };

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
        var pedido = new PedidoDto { Id = "123", UserId = 1, Total = 100 };

        _mockService.Setup(s => s.FindByIdAsync("123"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetPedidoById("123");

        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task GetPedidoById_AdminPuedeVerCualquierPedido_RetornaOk()
    {
        SetupUserClaims(2, "ADMIN");
        var pedido = new PedidoDto { Id = "123", UserId = 1, Total = 100 };

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
        var pedidoDto = new PedidoDto { Id = "123", Estado = "ENVIADO" };

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
        var pedidoDto = new PedidoDto
        {
            Id = "123",
            UserId = 1,
            Total = 175
        };

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.CreatePedido(requestDto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task UpdatePedidoEstado_TransicionesValidas_RetornaOk()
    {
        var pedidoDto = new PedidoDto { Id = "123", Estado = "PROCESANDO" };

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "PROCESANDO"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "PROCESANDO" });

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task UpdatePedidoEstado_CancelarPedido_RetornaOk()
    {
        var pedidoDto = new PedidoDto { Id = "123", Estado = "CANCELADO" };

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
}
