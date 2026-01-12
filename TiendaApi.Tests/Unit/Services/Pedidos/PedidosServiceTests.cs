using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Pedidos;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Validators.Pedidos;
using TiendaApi.Apis.WebSockets.Pedidos;

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
    private Mock<IValidator<PedidoRequestDto>> _mockPedidoValidator = null!;
    private Mock<IValidator<PedidoItemRequestDto>> _mockItemValidator = null!;
    private IPedidosService _service = null!;

    private void CreateService()
    {
        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockWebSocketHandler.Object,
            _mockPedidoValidator.Object,
            _mockItemValidator.Object
        );
    }

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
        _mockPedidoValidator = new Mock<IValidator<PedidoRequestDto>>();
        _mockItemValidator = new Mock<IValidator<PedidoItemRequestDto>>();

        // Setup default configuration
        _mockConfiguration.Setup(c => c["Smtp:AdminEmail"]).Returns("admin@test.com");
        
        // Configuración por defecto: validación pasa
        _mockPedidoValidator.Setup(v => v.ValidateAsync(It.IsAny<PedidoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockItemValidator.Setup(v => v.ValidateAsync(It.IsAny<PedidoItemRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        CreateService();
    }

    [Test]
    public async Task FindAllAsync_DebeRetornarTodosLosPedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new() { UserId = 1, Total = 100 },
            new() { UserId = 2, Total = 200 }
        };

        _mockPedidosRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(pedidos);

        // Act
        var result = await _service.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task CreateAsync_ConItemsValidos_DebeRetornarPedidoCreado()
    {
        // Arrange
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
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

        var pedidoGuardado = new Pedido
        {
            UserId = userId,
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, NombreProducto = "Test Product", Cantidad = 2, Precio = 50, Subtotal = 100 }
            },
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        // Act
        var result = await _service.CreateAsync(userId, pedidoDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_DebeRetornarErrorValidacion()
    {
        // Arrange
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>()
        };

        _mockPedidoValidator.Setup(v => v.ValidateAsync(pedidoDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Items", "El pedido debe contener al menos un artículo")
            }));

        // Re-crear servicio con mock configurado
        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockWebSocketHandler.Object,
            _mockPedidoValidator.Object,
            _mockItemValidator.Object
        );

        // Act
        var result = await _service.CreateAsync(userId, pedidoDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Test]
    public async Task CreateAsync_ConCantidadInvalida_DebeRetornarErrorValidacion()
    {
        // Arrange
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 0 }
            }
        };

        _mockItemValidator.Setup(v => v.ValidateAsync(pedidoDto.Items[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Cantidad", "La cantidad debe ser mayor a 0")
            }));

        // Re-crear servicio con mock configurado
        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockWebSocketHandler.Object,
            _mockPedidoValidator.Object,
            _mockItemValidator.Object
        );

        // Act
        var result = await _service.CreateAsync(userId, pedidoDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_DebeRetornarNotFound()
    {
        // Arrange
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 999, Cantidad = 2 }
            }
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // Act
        var result = await _service.CreateAsync(userId, pedidoDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Test]
    public async Task CreateAsync_ConStockInsuficiente_DebeRetornarBusinessRule()
    {
        // Arrange
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 20 }
            }
        };

        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            Precio = 50,
            Stock = 10
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        // Act
        var result = await _service.CreateAsync(userId, pedidoDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Test]
    public async Task UpdateEstadoAsync_ConEstadoValido_DebeRetornarPedidoActualizado()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var pedidoActualizado = new Pedido
        {
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.ENVIADO
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);
        _mockPedidosRepo.Setup(r => r.UpdateAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoActualizado);

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, "ENVIADO");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(PedidoEstado.ENVIADO);
    }

    [Test]
    public async Task UpdateEstadoAsync_ConEstadoInvalido_DebeRetornarErrorValidacion()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId().ToString();

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, "ESTADO_INVALIDO");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Test]
    public async Task UpdateEstadoAsync_ConPedidoNoExistente_DebeRetornarNotFound()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId().ToString();

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        // Act
        var result = await _service.UpdateEstadoAsync(pedidoId, "ENVIADO");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
