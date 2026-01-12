using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using TiendaApi.Dtos.Pedidos;
using TiendaApi.Errors;
using TiendaApi.Models;
using TiendaApi.Repositories.Pedidos;
using TiendaApi.Repositories.Productos;
using TiendaApi.Services.Cache;
using TiendaApi.Services.Email;
using TiendaApi.Services.Pedidos;
using TiendaApi.WebSockets.Pedidos;

namespace TiendaApi.Tests.Unit.Services.Pedidos;

/// <summary>
/// Tests unitarios para PedidosService usando Result Pattern
/// Prueba lógica de negocio, validación de stock y manejo de errores
/// </summary>
public class PedidosServiceTests
{
    private Mock<IPedidosRepository> _mockPedidosRepo = null!;
    private Mock<IProductoRepository> _mockProductoRepo = null!;
    private Mock<ILogger<PedidosService>> _mockLogger = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<PedidoWebSocketHandler> _mockWebSocketHandler = null!;
    private IPedidosService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockPedidosRepo = new Mock<IPedidosRepository>();
        _mockProductoRepo = new Mock<IProductoRepository>();
        _mockLogger = new Mock<ILogger<PedidosService>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockWebSocketHandler = new Mock<PedidoWebSocketHandler>(Mock.Of<ILogger<PedidoWebSocketHandler>>());

        // Setup default configuration
        _mockConfiguration.Setup(c => c["Smtp:AdminEmail"]).Returns("admin@test.com");

        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockWebSocketHandler.Object
        );
    }

    [Test]
    public async Task FindAllAsync_DebeRetornarTodosLosPedidos()
    {
        // Arrange
        var pedidoId1 = ObjectId.GenerateNewId();
        var pedidoId2 = ObjectId.GenerateNewId();
        var pedidos = new List<Pedido>
        {
            new() { _id = pedidoId1, UserId = 1, Total = 100 },
            new() { _id = pedidoId2, UserId = 2, Total = 200 }
        };

        _mockPedidosRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(pedidos);

        // Act
        var result = await _service.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _mockPedidosRepo.Verify(r => r.FindAllAsync(), Times.Once);
    }

    [Test]
    public async Task FindByIdAsync_ConIdExistente_DebeRetornarPedido()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId();
        var pedido = new Pedido { _id = pedidoId, UserId = 1, Total = 100 };

        _mockCacheService.Setup(c => c.GetAsync<PedidoDto>(It.IsAny<string>()))
            .ReturnsAsync((PedidoDto?)null);
        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId.ToString()))
            .ReturnsAsync(pedido);

        // Act
        var result = await _service.FindByIdAsync(pedidoId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pedidoId.ToString());
        _mockPedidosRepo.Verify(r => r.FindByIdAsync(pedidoId.ToString()), Times.Once);
    }

    [Test]
    public async Task FindByIdAsync_ConIdNoExistente_DebeRetornarErrorNoEncontrado()
    {
        // Arrange
        var pedidoId = "999";
        _mockCacheService.Setup(c => c.GetAsync<PedidoDto>(It.IsAny<string>()))
            .ReturnsAsync((PedidoDto?)null);
        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        // Act
        var result = await _service.FindByIdAsync(pedidoId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_DebeRetornarErrorValidacion()
    {
        // Arrange
        var userId = 1L;
        var dto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };

        // Act
        var result = await _service.CreateAsync(userId, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("al menos un producto");
    }

    [Test]
    public async Task CreateAsync_ConCantidadInvalida_DebeRetornarErrorValidacion()
    {
        // Arrange
        var userId = 1L;
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 0 }
            }
        };

        // Act
        var result = await _service.CreateAsync(userId, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("debe ser mayor que 0");
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_DebeRetornarErrorNoEncontrado()
    {
        // Arrange
        var userId = 1L;
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 999, Cantidad = 1 }
            }
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // Act
        var result = await _service.CreateAsync(userId, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("no encontrado");
    }

    [Test]
    public async Task CreateAsync_ConStockInsuficiente_DebeRetornarErrorReglaNegocio()
    {
        // Arrange
        var userId = 1L;
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 10 }
            }
        };

        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            Precio = 50,
            Stock = 5 // Insufficient stock
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        // Act
        var result = await _service.CreateAsync(userId, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
        result.Error.Message.Should().Contain("Stock insuficiente");
    }

    [Test]
    public async Task CreateAsync_ConDatosValidos_DebeCrearPedido()
    {
        // Arrange
        var userId = 1L;
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };

        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            Precio = 50,
            Stock = 10
        };

        var savedPedidoId = ObjectId.GenerateNewId();
        var savedPedido = new Pedido
        {
            _id = savedPedidoId,
            UserId = userId,
            Items = new List<PedidoItem>
            {
                new()
                {
                    ProductoId = 1,
                    NombreProducto = "Test Product",
                    Cantidad = 2,
                    Precio = 50,
                    Subtotal = 100
                }
            },
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(savedPedido);

        // Act
        var result = await _service.CreateAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(savedPedidoId.ToString());
        result.Value.Total.Should().Be(100);
        _mockProductoRepo.Verify(r => r.UpdateAsync(It.Is<Producto>(p => p.Stock == 8)), Times.Once);
        _mockPedidosRepo.Verify(r => r.SaveAsync(It.IsAny<Pedido>()), Times.Once);
    }

    [Test]
    public async Task UpdateEstadoAsync_ConEstadoInvalido_DebeRetornarErrorValidacion()
    {
        // Arrange
        var pedidoId = "123";
        var invalidEstado = "INVALID_ESTADO";

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, invalidEstado);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("Estado inválido");
    }

    [Test]
    public async Task UpdateEstadoAsync_ConPedidoNoExistente_DebeRetornarErrorNoEncontrado()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var nuevoEstado = PedidoEstado.PROCESANDO;

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, nuevoEstado);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Test]
    public async Task UpdateEstadoAsync_ConDatosValidos_DebeActualizarEstado()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId();
        var nuevoEstado = PedidoEstado.PROCESANDO;
        var pedido = new Pedido
        {
            _id = pedidoId,
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var updatedPedido = new Pedido
        {
            _id = pedidoId,
            UserId = 1,
            Total = 100,
            Estado = nuevoEstado
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId.ToString()))
            .ReturnsAsync(pedido);
        _mockPedidosRepo.Setup(r => r.UpdateAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(updatedPedido);

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId.ToString(), nuevoEstado);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(nuevoEstado);
        _mockPedidosRepo.Verify(r => r.UpdateAsync(It.Is<Pedido>(p => p.Estado == nuevoEstado)), Times.Once);
    }
}
