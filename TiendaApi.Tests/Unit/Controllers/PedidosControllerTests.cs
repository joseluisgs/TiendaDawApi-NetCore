using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Tests.Unit.Controllers;

/// <summary>
/// Tests unitarios para PedidosController.
/// Verifica los endpoints separados para administradores y usuarios.
/// </summary>
public class PedidosControllerTests
{
    private readonly Mock<IPedidosService> _mockService;
    private PedidosController _controller = null!;

    public PedidosControllerTests()
    {
        _mockService = new Mock<IPedidosService>();
        _controller = new PedidosController(_mockService.Object, Mock.Of<ILogger<PedidosController>>());
    }

    private static DestinatarioDto CreateValidDestinatario() => new()
    {
        NombreCompleto = "Test Destinatario",
        Email = "test@email.com",
        Direccion = new DireccionDto
        {
            Calle = "Calle Test",
            Ciudad = "Madrid",
            Pais = "España"
        }
    };

    private void SetupUserClaims(long userId, string role = "USER")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller = new PedidosController(_mockService.Object, Mock.Of<ILogger<PedidosController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            }
        };
    }

    #region ========== ADMIN ENDPOINT TESTS ==========

    #region GetAllPedidos (Admin)

    [Test]
    public async Task GetAllPedidos_AdminAutenticado_RetornaOk()
    {
        SetupUserClaims(1, "ADMIN");
        var pedidos = new List<PedidoDto>
        {
            new("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow),
            new("456", 2, CreateValidDestinatario(), new List<PedidoItemDto>(), 200m, "ENTREGADO", null, DateTime.UtcNow)
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

    #region GetAllPedidosPaged (Admin)

    [Test]
    public async Task GetAllPedidosPaged_AdminAutenticado_RetornaOk()
    {
        SetupUserClaims(1, "ADMIN");
        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = new List<PedidoDto>
            {
                new("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow)
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(0, 10))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult));

        var result = await _controller.GetAllPedidosPaged(1, 10);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Test]
    public async Task GetAllPedidosPaged_ConParametros_RetornaOk()
    {
        SetupUserClaims(1, "ADMIN");
        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = new List<PedidoDto>(),
            TotalCount = 0,
            Page = 5,
            PageSize = 20
        };

        _mockService.Setup(s => s.FindAllPagedAsync(4, 20))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult));

        var result = await _controller.GetAllPedidosPaged(5, 20);

        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetPedidoById (Admin)

    [Test]
    public async Task GetPedidoById_AdminPuedeVerCualquierPedido_RetornaOk()
    {
        SetupUserClaims(2, "ADMIN");
        var pedido = new PedidoDto("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.FindByIdAsync("123"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetPedidoById("123");

        result.Should().BeOfType<OkObjectResult>();
        var returnedPedido = (result as OkObjectResult)!.Value.Should().BeAssignableTo<PedidoDto>().Subject;
        returnedPedido.Id.Should().Be("123");
    }

    [Test]
    public async Task GetPedidoById_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1, "ADMIN");
        var error = new NotFoundError("Pedido no encontrado");

        _mockService.Setup(s => s.FindByIdAsync("999"))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.GetPedidoById("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdatePedidoAdmin (Admin)

    [Test]
    public async Task UpdatePedidoAdmin_ConDtoValido_RetornaOk()
    {
        SetupUserClaims(1, "ADMIN");
        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva dirección", Estado = "PROCESANDO" };
        var pedidoDto = new PedidoDto("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PROCESANDO", "Nueva dirección", DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateAdminAsync("123", updateDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdatePedidoAdmin("123", updateDto);

        result.Should().BeOfType<OkObjectResult>();
        var returnedPedido = (result as OkObjectResult)!.Value.Should().BeAssignableTo<PedidoDto>().Subject;
        returnedPedido.Estado.Should().Be("PROCESANDO");
    }

    [Test]
    public async Task UpdatePedidoAdmin_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1, "ADMIN");
        var updateDto = new UpdatePedidoDto();

        _mockService.Setup(s => s.UpdateAdminAsync("999", updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new NotFoundError("Pedido no encontrado")));

        var result = await _controller.UpdatePedidoAdmin("999", updateDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeletePedidoAdmin (Admin)

    [Test]
    public async Task DeletePedidoAdmin_PedidoExistente_RetornaNoContent()
    {
        SetupUserClaims(1, "ADMIN");

        _mockService.Setup(s => s.DeleteAdminAsync("123"))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.DeletePedidoAdmin("123");

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeletePedidoAdmin_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1, "ADMIN");

        _mockService.Setup(s => s.DeleteAdminAsync("999"))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new NotFoundError("Pedido no encontrado")));

        var result = await _controller.DeletePedidoAdmin("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdatePedidoEstado (Admin)

    [Test]
    public async Task UpdatePedidoEstado_ConEstadoValido_RetornaOk()
    {
        SetupUserClaims(1, "ADMIN");
        var pedidoDto = new PedidoDto("123", 0, CreateValidDestinatario(), new List<PedidoItemDto>(), 0m, "ENVIADO", null, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "ENVIADO"))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "ENVIADO" });

        result.Should().BeOfType<OkObjectResult>();
        var returnedPedido = (result as OkObjectResult)!.Value.Should().BeAssignableTo<PedidoDto>().Subject;
        returnedPedido.Estado.Should().Be("ENVIADO");
    }

    [Test]
    public async Task UpdatePedidoEstado_ConEstadoInvalido_RetornaBadRequest()
    {
        SetupUserClaims(1, "ADMIN");
        var error = ValidationError.Create("Estado inválido");

        _mockService.Setup(s => s.UpdateEstadoAsync("123", "INVALIDO"))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.UpdatePedidoEstado("123", new UpdateEstadoDto { Estado = "INVALIDO" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #endregion

    #region ========== USER ENDPOINT TESTS ==========

    #region GetMyPedidos (User - Sin paginación)

    [Test]
    public async Task GetMyPedidos_ConPedidos_RetornaOk()
    {
        SetupUserClaims(1);
        var pedidos = new List<PedidoDto>
        {
            new("1", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow),
            new("2", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 200m, "PENDIENTE", null, DateTime.UtcNow)
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
        var controller = new PedidosController(_mockService.Object, Mock.Of<ILogger<PedidosController>>());

        var result = await controller.GetMyPedidos();

        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region GetMyPedidosPaged (User - Con paginación)

    [Test]
    public async Task GetMyPedidosPaged_ConParametros_RetornaOk()
    {
        SetupUserClaims(1);
        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = new List<PedidoDto>
            {
                new("1", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow)
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindMyPedidosAsync(1, 0, 10))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult));

        var result = await _controller.GetMyPedidosPaged(1, 10);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Test]
    public async Task GetMyPedidosPaged_ValoresDefecto_RetornaOk()
    {
        SetupUserClaims(1);
        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = new List<PedidoDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindMyPedidosAsync(1, 0, 10))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult));

        var result = await _controller.GetMyPedidosPaged();

        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region CreateMyPedido (User)

    [Test]
    public async Task CreateMyPedido_ConDtoValido_RetornaCreated()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var pedidoDto = new PedidoDto("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.CreateMyPedido(requestDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("GetMyPedidoById");
    }

    [Test]
    public async Task CreateMyPedido_ConDestinatario_RetornaCreated()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "María García",
                Email = "maria@email.com",
                Telefono = "+34612345678",
                Direccion = new DireccionDto
                {
                    Calle = "Gran Vía",
                    Numero = "42",
                    Ciudad = "Madrid",
                    Provincia = "Madrid",
                    Pais = "España",
                    CodigoPostal = "28013"
                }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var destinatarioDto = new DestinatarioDto
        {
            NombreCompleto = "María García",
            Email = "maria@email.com",
            Telefono = "+34612345678",
            Direccion = new DireccionDto
            {
                Calle = "Gran Vía",
                Numero = "42",
                Ciudad = "Madrid",
                Provincia = "Madrid",
                Pais = "España",
                CodigoPostal = "28013"
            }
        };
        var pedidoDto = new PedidoDto("123", 1, destinatarioDto, new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.CreateMyPedido(requestDto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task CreateMyPedido_SinItems_RetornaBadRequest()
    {
        SetupUserClaims(1);
        var requestDto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };
        var error = ValidationError.Create("El pedido debe contener al menos un artículo");

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.CreateMyPedido(requestDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreateMyPedido_StockInsuficiente_RetornaBadRequest()
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

        var result = await _controller.CreateMyPedido(requestDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreateMyPedido_SinAutenticacion_RetornaUnauthorized()
    {
        var controller = new PedidosController(_mockService.Object, Mock.Of<ILogger<PedidosController>>());
        var requestDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };

        var result = await controller.CreateMyPedido(requestDto);

        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region GetMyPedidoById (User)

    [Test]
    public async Task GetMyPedidoById_Propietario_RetornaOk()
    {
        SetupUserClaims(1);
        var pedido = new PedidoDto("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.FindMyPedidoAsync("123", 1))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetMyPedidoById("123");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPedido = okResult.Value.Should().BeAssignableTo<PedidoDto>().Subject;
        returnedPedido.Id.Should().Be("123");
    }

    [Test]
    public async Task GetMyPedidoById_NoPropietario_RetornaForbidden()
    {
        SetupUserClaims(2);
        var pedido = new PedidoDto("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.FindMyPedidoAsync("123", 2))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new ForbiddenError("No eres propietario")));

        var result = await _controller.GetMyPedidoById("123");

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task GetMyPedidoById_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1);
        var error = new NotFoundError("Pedido no encontrado");

        _mockService.Setup(s => s.FindMyPedidoAsync("999", 1))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(error));

        var result = await _controller.GetMyPedidoById("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdateMyPedido (User)

    [Test]
    public async Task UpdateMyPedido_PropietarioYPendiente_RetornaOk()
    {
        SetupUserClaims(1);
        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva dirección" };
        var pedidoDto = new PedidoDto("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 100m, "PENDIENTE", "Nueva dirección", DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateMyPedidoAsync("123", 1, updateDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.UpdateMyPedido("123", updateDto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task UpdateMyPedido_NoPropietario_RetornaForbidden()
    {
        SetupUserClaims(2);
        var updateDto = new UpdatePedidoDto();

        _mockService.Setup(s => s.UpdateMyPedidoAsync("123", 2, updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new ForbiddenError("No eres propietario")));

        var result = await _controller.UpdateMyPedido("123", updateDto);

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task UpdateMyPedido_PedidoNoPendiente_RetornaBadRequest()
    {
        SetupUserClaims(1);
        var updateDto = new UpdatePedidoDto();

        _mockService.Setup(s => s.UpdateMyPedidoAsync("123", 1, updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(
                new ValidationError("No se puede actualizar un pedido en estado ENVIADO", new Dictionary<string, string[]>())));

        var result = await _controller.UpdateMyPedido("123", updateDto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdateMyPedido_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1);
        var updateDto = new UpdatePedidoDto();

        _mockService.Setup(s => s.UpdateMyPedidoAsync("999", 1, updateDto))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new NotFoundError("Pedido no encontrado")));

        var result = await _controller.UpdateMyPedido("999", updateDto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteMyPedido (User)

    [Test]
    public async Task DeleteMyPedido_PropietarioYPendiente_RetornaNoContent()
    {
        SetupUserClaims(1);

        _mockService.Setup(s => s.DeleteMyPedidoAsync("123", 1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.DeleteMyPedido("123");

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeleteMyPedido_NoPropietario_RetornaForbidden()
    {
        SetupUserClaims(2);

        _mockService.Setup(s => s.DeleteMyPedidoAsync("123", 2))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new ForbiddenError("No eres propietario")));

        var result = await _controller.DeleteMyPedido("123");

        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(403);
    }

    [Test]
    public async Task DeleteMyPedido_PedidoNoPendiente_RetornaBadRequest()
    {
        SetupUserClaims(1);

        _mockService.Setup(s => s.DeleteMyPedidoAsync("123", 1))
            .ReturnsAsync(UnitResult.Failure<DomainError>(
                new ValidationError("No se puede eliminar un pedido en estado ENVIADO", new Dictionary<string, string[]>())));

        var result = await _controller.DeleteMyPedido("123");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    [Test]
    public async Task DeleteMyPedido_PedidoNoExistente_RetornaNotFound()
    {
        SetupUserClaims(1);

        _mockService.Setup(s => s.DeleteMyPedidoAsync("999", 1))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new NotFoundError("Pedido no encontrado")));

        var result = await _controller.DeleteMyPedido("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region ========== TESTS ADICIONALES ==========

    [Test]
    public async Task CreateMyPedido_ErrorInterno_Retorna500()
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

        var result = await _controller.CreateMyPedido(requestDto);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task CreateMyPedido_MultiplesItems_RetornaCreated()
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
        var pedidoDto = new PedidoDto("123", 1, CreateValidDestinatario(), new List<PedidoItemDto>(), 175m, "PENDIENTE", null, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(1, requestDto))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedidoDto));

        var result = await _controller.CreateMyPedido(requestDto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task GetMyPedidosPaged_SinAutenticacion_RetornaUnauthorized()
    {
        var controller = new PedidosController(_mockService.Object, Mock.Of<ILogger<PedidosController>>());

        var result = await controller.GetMyPedidosPaged();

        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(401);
    }

    #endregion
}
