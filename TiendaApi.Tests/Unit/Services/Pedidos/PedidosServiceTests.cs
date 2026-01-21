using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using Npgsql;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Exceptions;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Pedidos;
using TiendaApi.Api.Validators.Pedidos;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Tests.Unit.Services.Pedidos;

/// <summary>
/// Tests unitarios para PedidosService usando enfoque híbrido: Serializable + Retry
/// </summary>
public class PedidosServiceTests
{
    private Mock<IPedidosRepository> _mockPedidosRepo = null!;
    private Mock<IProductoRepository> _mockProductoRepo = null!;
    private Mock<ILogger<PedidosService>> _mockLogger = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<PedidosWebSocketHandler> _mockWebSocketHandler = null!;
    private Mock<IValidator<PedidoRequestDto>> _mockPedidoValidator = null!;
    private Mock<IValidator<PedidoItemRequestDto>> _mockItemValidator = null!;
    private Mock<IDbContextTransaction> _mockTransaction = null!;
    private Mock<IHubContext<PedidosHub>> _mockHubContext = null!;
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
            _mockHubContext.Object,
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
        _mockHubContext = new Mock<IHubContext<PedidosHub>>();
        _mockWebSocketHandler = new Mock<PedidosWebSocketHandler>(
            Mock.Of<ILogger<PedidosWebSocketHandler>>(),
            Mock.Of<IJwtTokenExtractor>());
        _mockPedidoValidator = new Mock<IValidator<PedidoRequestDto>>();
        _mockItemValidator = new Mock<IValidator<PedidoItemRequestDto>>();
        _mockTransaction = new Mock<IDbContextTransaction>();

        _mockConfiguration.Setup(c => c["Smtp:AdminEmail"]).Returns("admin@test.com");

        var wsSectionMock = new Mock<IConfigurationSection>();
        wsSectionMock.Setup(s => s.Value).Returns("5");
        _mockConfiguration.Setup(c => c.GetSection("WebSocket:RoleCacheTTLMinutes"))
            .Returns(wsSectionMock.Object);

        _mockWebSocketHandler = new Mock<PedidosWebSocketHandler>(
            Mock.Of<ILogger<PedidosWebSocketHandler>>(),
            Mock.Of<IJwtTokenExtractor>(),
            _mockCacheService.Object,
            _mockConfiguration.Object);

        _mockPedidoValidator.Setup(v => v.ValidateAsync(It.IsAny<PedidoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockItemValidator.Setup(v => v.ValidateAsync(It.IsAny<PedidoItemRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockItemValidator.Setup(v => v.ValidateAsync(It.IsAny<PedidoItemRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockProductoRepo.Setup(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ReturnsAsync(_mockTransaction.Object);

        _mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()));
        _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()));

        CreateService();
    }

    [Test]
    public async Task FindAllAsync_DebeRetornarTodosLosPedidos()
    {
        var pedidos = new List<Pedido>
        {
            new() { UserId = 1, Total = 100 },
            new() { UserId = 2, Total = 200 }
        };

        _mockPedidosRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(pedidos);

        var result = await _service.FindAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task CreateAsync_ConItemsValidos_DebeRetornarPedidoCreado()
    {
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
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_DebeRetornarErrorValidacion()
    {
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

        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockWebSocketHandler.Object,
            _mockHubContext.Object,
            _mockPedidoValidator.Object,
            _mockItemValidator.Object
        );

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public async Task CreateAsync_ConCantidadInvalida_DebeRetornarErrorValidacion()
    {
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

        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockWebSocketHandler.Object,
            _mockHubContext.Object,
            _mockPedidoValidator.Object,
            _mockItemValidator.Object
        );

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_DebeRetornarNotFound()
    {
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

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ConStockInsuficiente_DebeRetornarBusinessRule()
    {
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

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ErrorDeSerializacion_DebeRetornarConflictTrasMaximosReintentos()
    {
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

        var npgsqlException = new NpgsqlException("40001: serialization_failure");

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(npgsqlException)
            .ThrowsAsync(npgsqlException)
            .ThrowsAsync(npgsqlException)
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public async Task CreateAsync_ReintentoExitosoTrasErrorDeSerializacion_DebeRetornarPedido()
    {
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

        var npgsqlException = new NpgsqlException("40001: serialization_failure");

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(npgsqlException)
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_MultiplesItems_DebeDecrementarStockParaCadaUno()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 3 }
            }
        };

        var producto1 = new Producto
        {
            Id = 1,
            Nombre = "Product 1",
            Precio = 50,
            Stock = 10
        };

        var producto2 = new Producto
        {
            Id = 2,
            Nombre = "Product 2",
            Precio = 25,
            Stock = 20
        };

        var pedidoGuardado = new Pedido
        {
            UserId = userId,
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, NombreProducto = "Product 1", Cantidad = 2, Precio = 50, Subtotal = 100 },
                new() { ProductoId = 2, NombreProducto = "Product 2", Cantidad = 3, Precio = 25, Subtotal = 75 }
            },
            Total = 175,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto1);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(2))
            .ReturnsAsync(producto2);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync((Producto p) => p);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(175);
        _mockProductoRepo.Verify(r => r.UpdateAsync(It.IsAny<Producto>()), Times.Exactly(2));
    }

    [Test]
    public async Task UpdateEstadoAsync_ConEstadoValido_DebeRetornarPedidoActualizado()
    {
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

        var result = await _service.UpdateEstadoAsync(pedidoId, "ENVIADO");

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(PedidoEstado.ENVIADO);
    }

    [Test]
    public async Task UpdateEstadoAsync_ConEstadoInvalido_DebeRetornarErrorValidacion()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();

        var result = await _service.UpdateEstadoAsync(pedidoId, "ESTADO_INVALIDO");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public async Task UpdateEstadoAsync_ConPedidoNoExistente_DebeRetornarNotFound()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        var result = await _service.UpdateEstadoAsync(pedidoId, "ENVIADO");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #region Tests de Deteccion de Errores 40001

    [Test]
    public async Task CreateAsync_DbUpdateExceptionCon40001_DebeReintentar()
    {
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

        var npgsqlException = new NpgsqlException("40001: could not serialize");
        var dbUpdateException = new DbUpdateException("Error", npgsqlException);

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
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(new SerializationFailureException("serialization failure", dbUpdateException))
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
    }

    [Test]
    public async Task CreateAsync_NpgsqlExceptionConSerializacionIngles_DebeReintentar()
    {
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

        var npgsqlException = new NpgsqlException("40001: could not serialize access due to concurrent update");

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(new SerializationFailureException("serialization failure", npgsqlException))
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_NpgsqlExceptionConSerializacionEspanol_DebeReintentar()
    {
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

        var npgsqlException = new NpgsqlException("error de serializacion al actualizar");

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(new SerializationFailureException("error de serializacion", npgsqlException))
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Tests de Comportamiento de Transaccion

    [Test]
    public async Task CreateAsync_Exito_DebeLlamarCommit()
    {
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
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        await _service.CreateAsync(userId, pedidoDto);

        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_ValidationFails_DebeLlamarRollback()
    {
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

        _service = new PedidosService(
            _mockPedidosRepo.Object,
            _mockProductoRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockWebSocketHandler.Object,
            _mockHubContext.Object,
            _mockPedidoValidator.Object,
            _mockItemValidator.Object
        );

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_ProductoNoExiste_DebeLlamarRollback()
    {
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

        await _service.CreateAsync(userId, pedidoDto);

        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_StockInsuficiente_DebeLlamarRollback()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 100 }
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

        await _service.CreateAsync(userId, pedidoDto);

        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Tests de Retry con Diferentes Intentos

    [Test]
    public async Task CreateAsync_PrimerIntentoFalla_SegundoExito_RetornaPedido()
    {
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
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var npgsqlException = new NpgsqlException("40001: serialization_failure");

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(npgsqlException)
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_DosIntentosFallan_TerceroExito_RetornaPedido()
    {
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
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var npgsqlException = new NpgsqlException("40001: serialization_failure");

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(npgsqlException)
            .ThrowsAsync(npgsqlException)
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Tests de Multiples Items con Fallo

    [Test]
    public async Task CreateAsync_TercerItemFalla_TransaccionHaceRollback()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 3 },
                new() { ProductoId = 3, Cantidad = 100 }
            }
        };

        var producto1 = new Producto { Id = 1, Nombre = "Product 1", Precio = 50, Stock = 10 };
        var producto2 = new Producto { Id = 2, Nombre = "Product 2", Precio = 25, Stock = 20 };
        var producto3 = new Producto { Id = 3, Nombre = "Product 3", Precio = 30, Stock = 5 };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(producto1);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(2)).ReturnsAsync(producto2);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(3)).ReturnsAsync(producto3);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync((Producto p) => p);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
        _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_TodosItemsExito_DecrementaStockCorrectamente()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 1 }
            }
        };

        var producto1 = new Producto { Id = 1, Nombre = "Product 1", Precio = 50, Stock = 10 };
        var producto2 = new Producto { Id = 2, Nombre = "Product 2", Precio = 25, Stock = 5 };

        var pedidoGuardado = new Pedido
        {
            UserId = userId,
            Total = 125,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(producto1);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(2)).ReturnsAsync(producto2);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync((Producto p) => p);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(125);
        _mockProductoRepo.Verify(r => r.UpdateAsync(It.Is<Producto>(p => p.Id == 1 && p.Stock == 8)), Times.Once);
        _mockProductoRepo.Verify(r => r.UpdateAsync(It.Is<Producto>(p => p.Id == 2 && p.Stock == 4)), Times.Once);
    }

    #endregion

    #region Tests de Casos Borde

    [Test]
    public async Task CreateAsync_ItemConCantidadExactaStock_Exito()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 5 }
            }
        };

        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            Precio = 50,
            Stock = 5
        };

        var pedidoGuardado = new Pedido
        {
            UserId = userId,
            Total = 250,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.Is<Producto>(p => p.Stock == 0)))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(250);
    }

    [Test]
    public async Task CreateAsync_UnSoloItem_DebeFuncionarCorrectamente()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 1 }
            }
        };

        var producto = new Producto
        {
            Id = 1,
            Nombre = "Single Item Product",
            Precio = 99.99m,
            Stock = 100
        };

        var pedidoGuardado = new Pedido
        {
            UserId = userId,
            Total = 99.99m,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(pedidoGuardado);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(99.99m);
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ErrorNoDeSerializacion_NoReintenta()
    {
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

        var genericException = new SerializationFailureException("connection timeout",
            new NpgsqlException("connection timeout"));

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(genericException)
            .ThrowsAsync(genericException)
            .ThrowsAsync(genericException)
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public async Task CreateAsync_MaxRetriesConDiferentesDelays_EsperaCorrectamente()
    {
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

        var npgsqlException = new NpgsqlException("40001: serialization_failure");

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        _mockProductoRepo.SetupSequence(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ThrowsAsync(new SerializationFailureException("serialization failure", npgsqlException))
            .ThrowsAsync(new SerializationFailureException("serialization failure", npgsqlException))
            .ThrowsAsync(new SerializationFailureException("serialization failure", npgsqlException))
            .ReturnsAsync(_mockTransaction.Object);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Maximos reintentos alcanzados")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    #endregion

    #region Tests de FindAllPagedAsync (Admin)

    [Test]
    public async Task FindAllPagedAsync_ConParametrosValidos_RetornaPedidosPaginados()
    {
        var pedidos = new List<Pedido>
        {
            new() { UserId = 1, Total = 100 },
            new() { UserId = 2, Total = 200 },
            new() { UserId = 3, Total = 300 }
        };

        _mockPedidosRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(pedidos);

        var result = await _service.FindAllPagedAsync(0, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_Pagina2_RetornaItemsCorrectos()
    {
        var pedidos = new List<Pedido>
        {
            new() { UserId = 1, Total = 100 },
            new() { UserId = 2, Total = 200 },
            new() { UserId = 3, Total = 300 }
        };

        _mockPedidosRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(pedidos);

        var result = await _service.FindAllPagedAsync(1, 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Page.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_SinPedidos_RetornaListaVacia()
    {
        _mockPedidosRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(new List<Pedido>());

        var result = await _service.FindAllPagedAsync(0, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region Tests de FindByUserIdAsync (User - Sin paginación)

    [Test]
    public async Task FindByUserIdAsync_ConPedidos_RetornaLista()
    {
        var pedidos = new List<Pedido>
        {
            new() { UserId = 1, Total = 100 },
            new() { UserId = 1, Total = 200 }
        };

        _mockPedidosRepo.Setup(r => r.FindByUserIdAsync(1))
            .ReturnsAsync(pedidos);

        var result = await _service.FindByUserIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task FindByUserIdAsync_SinPedidos_RetornaListaVacia()
    {
        _mockPedidosRepo.Setup(r => r.FindByUserIdAsync(1))
            .ReturnsAsync(new List<Pedido>());

        var result = await _service.FindByUserIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Tests de FindMyPedidosAsync (User - Paginado)

    [Test]
    public async Task FindMyPedidosAsync_ConParametros_RetornaPagedResult()
    {
        var pedidos = new List<Pedido>
        {
            new() { UserId = 1, Total = 100 },
            new() { UserId = 1, Total = 200 }
        };

        _mockPedidosRepo.Setup(r => r.FindByUserIdPagedAsync(1, 1, 10))
            .ReturnsAsync((pedidos, 2));

        var result = await _service.FindMyPedidosAsync(1, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
    }

    #endregion

    #region Tests de FindMyPedidoAsync (User)

    [Test]
    public async Task FindMyPedidoAsync_Propietario_RetornaPedido()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedido = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedido);

        var result = await _service.FindMyPedidoAsync(pedidoId, 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pedidoId);
    }

    [Test]
    public async Task FindMyPedidoAsync_NoPropietario_RetornaForbidden()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedido = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedido);

        var result = await _service.FindMyPedidoAsync(pedidoId, 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ForbiddenError>();
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoNoExistente_RetornaNotFound()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        var result = await _service.FindMyPedidoAsync(pedidoId, 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion

    #region Tests de UpdateAdminAsync (Admin)

    [Test]
    public async Task UpdateAdminAsync_PedidoExistente_RetornaOk()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva dirección", Estado = "PROCESANDO" };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);
        _mockPedidosRepo.Setup(r => r.UpdateAsync(It.IsAny<Pedido>()))
            .ReturnsAsync((Pedido p) => p);

        var result = await _service.UpdateAdminAsync(pedidoId, updateDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be("PROCESANDO");
    }

    [Test]
    public async Task UpdateAdminAsync_PedidoNoExistente_RetornaNotFound()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var updateDto = new UpdatePedidoDto();

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        var result = await _service.UpdateAdminAsync(pedidoId, updateDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion

    #region Tests de UpdateMyPedidoAsync (User)

    [Test]
    public async Task UpdateMyPedidoAsync_PropietarioYPendiente_RetornaOk()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva dirección" };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);
        _mockPedidosRepo.Setup(r => r.UpdateAsync(It.IsAny<Pedido>()))
            .ReturnsAsync((Pedido p) => p);

        var result = await _service.UpdateMyPedidoAsync(pedidoId, 1, updateDto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateMyPedidoAsync_NoPropietario_RetornaForbidden()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        var updateDto = new UpdatePedidoDto();

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);

        var result = await _service.UpdateMyPedidoAsync(pedidoId, 2, updateDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ForbiddenError>();
    }

    [Test]
    public async Task UpdateMyPedidoAsync_PedidoNoPendiente_RetornaValidationError()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.ENVIADO
        };

        var updateDto = new UpdatePedidoDto();

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);

        var result = await _service.UpdateMyPedidoAsync(pedidoId, 1, updateDto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    #endregion

    #region Tests de DeleteAdminAsync (Admin)

    [Test]
    public async Task DeleteAdminAsync_PedidoExistente_RetornaOk()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);
        _mockPedidosRepo.Setup(r => r.UpdateAsync(It.IsAny<Pedido>()))
            .ReturnsAsync((Pedido p) => p);

        var result = await _service.DeleteAdminAsync(pedidoId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoNoExistente_RetornaNotFound()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync((Pedido?)null);

        var result = await _service.DeleteAdminAsync(pedidoId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion

    #region Tests de DeleteMyPedidoAsync (User)

    [Test]
    public async Task DeleteMyPedidoAsync_PropietarioYPendiente_RetornaOk()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);
        _mockPedidosRepo.Setup(r => r.UpdateAsync(It.IsAny<Pedido>()))
            .ReturnsAsync((Pedido p) => p);

        var result = await _service.DeleteMyPedidoAsync(pedidoId, 1);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_NoPropietario_RetornaForbidden()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.PENDIENTE
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);

        var result = await _service.DeleteMyPedidoAsync(pedidoId, 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ForbiddenError>();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_PedidoNoPendiente_RetornaValidationError()
    {
        var pedidoId = ObjectId.GenerateNewId().ToString();
        var pedidoExistente = new Pedido
        {
            Id = ObjectId.Parse(pedidoId),
            UserId = 1,
            Total = 100,
            Estado = PedidoEstado.ENVIADO
        };

        _mockPedidosRepo.Setup(r => r.FindByIdAsync(pedidoId))
            .ReturnsAsync(pedidoExistente);

        var result = await _service.DeleteMyPedidoAsync(pedidoId, 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    #endregion

    #region Tests de Create con Destinatario

    [Test]
    public async Task CreateAsync_ConDestinatario_DebeGuardarDestinatario()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
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
                    CodigoPostal = "28013"
                }
            },
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

        Pedido? pedidoGuardado = null;
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .Callback<Pedido>(p => pedidoGuardado = p)
            .ReturnsAsync((Pedido p) => p);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        pedidoGuardado.Should().NotBeNull();
        pedidoGuardado!.Destinatario.Should().NotBeNull();
        pedidoGuardado.Destinatario!.NombreCompleto.Should().Be("María García");
        pedidoGuardado.Destinatario.Direccion.Should().NotBeNull();
        pedidoGuardado.Destinatario.Direccion!.Calle.Should().Be("Gran Vía");
    }

    [Test]
    public async Task CreateAsync_ConDestinatario_DebeTenerDestinatario()
    {
        long userId = 1;
        var pedidoDto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test Destinatario",
                Email = "test@test.com",
                Direccion = new DireccionDto { Calle = "Calle Test", Ciudad = "Madrid", Pais = "España" }
            },
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

        Pedido? pedidoGuardado = null;
        _mockPedidosRepo.Setup(r => r.SaveAsync(It.IsAny<Pedido>()))
            .Callback<Pedido>(p => pedidoGuardado = p)
            .ReturnsAsync((Pedido p) => p);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);

        var result = await _service.CreateAsync(userId, pedidoDto);

        result.IsSuccess.Should().BeTrue();
        pedidoGuardado.Should().NotBeNull();
        pedidoGuardado!.Destinatario.Should().NotBeNull();
        pedidoGuardado.Destinatario!.NombreCompleto.Should().Be("Test Destinatario");
        pedidoGuardado.Destinatario.Email.Should().Be("test@test.com");
        pedidoGuardado.Destinatario.Direccion.Should().NotBeNull();
        pedidoGuardado.Destinatario.Direccion!.Calle.Should().Be("Calle Test");
    }

    #endregion
}
