using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;
using TiendaApi.Services.Cache;
using TiendaApi.Services.Email;
using TiendaApi.Services.Pedidos;

namespace TiendaApi.Tests;

/// <summary>
/// Unit tests for PedidosService using Result Pattern
/// Tests business logic, stock validation, and error handling
/// </summary>
public class PedidosServiceTests
{
    private Mock<IPedidosRepository> _mockPedidosRepo = null!;
    private Mock<IProductoRepository> _mockProductoRepo = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<PedidosService>> _mockLogger = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private IPedidosService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockPedidosRepo = new Mock<IPedidosRepository>();
        _mockProductoRepo = new Mock<IProductoRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PedidosService>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration
        _mockConfiguration.Setup(c => c["Smtp:AdminEmail"]).Returns("admin@test.com");

        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object
        );
    }

    [Test]
    public async Task FindAllAsync_ShouldReturnAllPedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new() { Id = "1", UserId = 1, Total = 100 },
            new() { Id = "2", UserId = 2, Total = 200 }
        };
        var pedidoDtos = new List<PedidoDto>
        {
            new() { Id = "1", UserId = 1, Total = 100 },
            new() { Id = "2", UserId = 2, Total = 200 }
        };

        _mockPedidosRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(pedidos);
        _mockMapper.Setup(m => m.Map<IEnumerable<PedidoDto>>(pedidos))
            .Returns(pedidoDtos);

        // Act
        var result = await _service.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _mockPedidosRepo.Verify(r => r.FindAllAsync(), Times.Once);
    }

    [Test]
    public async Task FindByIdAsync_WithExistingId_ShouldReturnPedido()
    {
        // Arrange
        var pedidoId = "123";
        var pedido = new Pedido { Id = pedidoId, UserId = 1, Total = 100 };
        var pedidoDto = new PedidoDto { Id = pedidoId, UserId = 1, Total = 100 };

        _mockCacheService.Setup(c => c.GetAsync<PedidoDto>(It.IsAny<string>()))
            .ReturnsAsync((PedidoDto?)null);
        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedido);
        _mockMapper.Setup(m => m.Map<PedidoDto>(pedido))
            .Returns(pedidoDto);

        // Act
        var result = await _service.FindByIdAsync(pedidoId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pedidoId);
        _mockPedidosRepo.Verify(r => r.FindByIdAsync(pedidoId), Times.Once);
    }

    [Test]
    public async Task FindByIdAsync_WithNonExistingId_ShouldReturnNotFoundError()
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
        result.Error.Type.Should().Be(Common.ErrorType.NotFound);
    }

    [Test]
    public async Task CreateAsync_WithEmptyItems_ShouldReturnValidationError()
    {
        // Arrange
        var userId = 1L;
        var dto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };

        // Act
        var result = await _service.CreateAsync(userId, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
        result.Error.Message.Should().Contain("al menos un producto");
    }

    [Test]
    public async Task CreateAsync_WithInvalidQuantity_ShouldReturnValidationError()
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
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
        result.Error.Message.Should().Contain("debe ser mayor que 0");
    }

    [Test]
    public async Task CreateAsync_WithNonExistingProduct_ShouldReturnNotFoundError()
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
        result.Error.Type.Should().Be(Common.ErrorType.NotFound);
        result.Error.Message.Should().Contain("no encontrado");
    }

    [Test]
    public async Task CreateAsync_WithInsufficientStock_ShouldReturnBusinessRuleError()
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
        result.Error.Type.Should().Be(Common.ErrorType.BusinessRule);
        result.Error.Message.Should().Contain("Stock insuficiente");
    }

    [Test]
    public async Task CreateAsync_WithValidData_ShouldCreatePedido()
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

        var savedPedido = new Pedido
        {
            Id = "new-pedido-id",
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

        var pedidoDto = new PedidoDto
        {
            Id = "new-pedido-id",
            UserId = userId,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(savedPedido);
        _mockMapper.Setup(m => m.Map<PedidoDto>(savedPedido))
            .Returns(pedidoDto);

        // Act
        var result = await _service.CreateAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("new-pedido-id");
        result.Value.Total.Should().Be(100);
        _mockProductoRepo.Verify(r => r.UpdateAsync(It.Is<Producto>(p => p.Stock == 8)), Times.Once);
        _mockPedidosRepo.Verify(r => r.SaveAsync(It.IsAny<Pedido>()), Times.Once);
    }

    [Test]
    public async Task UpdateEstadoAsync_WithInvalidEstado_ShouldReturnValidationError()
    {
        // Arrange
        var pedidoId = "123";
        var invalidEstado = "INVALID_ESTADO";

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, invalidEstado);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
        result.Error.Message.Should().Contain("Estado inválido");
    }

    [Test]
    public async Task UpdateEstadoAsync_WithNonExistingPedido_ShouldReturnNotFoundError()
    {
        // Arrange
        var pedidoId = "999";
        var nuevoEstado = PedidoEstado.PROCESANDO;

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, nuevoEstado);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.NotFound);
    }

    [Test]
    public async Task UpdateEstadoAsync_WithValidData_ShouldUpdateEstado()
    {
        // Arrange
        var pedidoId = "123";
        var nuevoEstado = PedidoEstado.PROCESANDO;
        var pedido = new Pedido
        {
            Id = pedidoId,
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var updatedPedido = new Pedido
        {
            Id = pedidoId,
            UserId = 1,
            Total = 100,
            Estado = nuevoEstado
        };

        var pedidoDto = new PedidoDto
        {
            Id = pedidoId,
            UserId = 1,
            Total = 100,
            Estado = nuevoEstado
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedido);
        _mockPedidosRepo.Setup(r => r.UpdateAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(updatedPedido);
        _mockMapper.Setup(m => m.Map<PedidoDto>(updatedPedido))
            .Returns(pedidoDto);

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, nuevoEstado);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(nuevoEstado);
        _mockPedidosRepo.Verify(r => r.UpdateAsync(It.Is<Pedido>(p => p.Estado == nuevoEstado)), Times.Once);
    }
}
