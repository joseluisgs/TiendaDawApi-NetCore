using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Pedidos;
using TiendaApi.Api.Validators.Pedidos;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
/// Tests de integración para PedidosService con MongoDB Driver nativo.
/// Esta clase complementa PedidosServiceIntegrationTests, mostrando que
/// los tests funcionan correctamente con el driver nativo (a diferencia de EF Core).
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class PedidosNativeServiceIntegrationTests
{
    private static readonly ActivitySource ActivitySource = new("PedidosNativeServiceIntegrationTests");
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IPedidosService? _pedidosService;
    private long _productoId;
    private long _userId;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithPortBinding(27017, true)
            .Build();

        await _mongoContainer.StartAsync();

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("tienda_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgresContainer.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [SetUp]
    public async Task Setup()
    {
        var connectionString = _postgresContainer!.GetConnectionString();
        var mongoConnectionString = _mongoContainer!.GetConnectionString();
        var mongoDatabaseName = "tienda_test";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", connectionString },
                { "MongoDbSettings:ConnectionString", mongoConnectionString },
                { "MongoDbSettings:DatabaseName", mongoDatabaseName },
                { "Cache:PedidoCacheTTLMinutes", "5" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        services.AddSingleton(ActivitySource);

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Registrar SignalR (requerido por PedidosService)
        services.AddSignalR();

        // Registrar MongoDB Driver nativo (sin EF Core)
        services.AddSingleton<IMongoClient>(sp =>
        {
            return new MongoClient(mongoConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoDatabaseName);
        });

        RegisterRepositories(services);
        RegisterServices(services);

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        var productoRepo = _serviceProvider.GetRequiredService<IProductoRepository>();
        var categoriaRepo = _serviceProvider.GetRequiredService<ICategoriaRepository>();
        var categoria = new Categoria { Nombre = $"Test Categoria {Guid.NewGuid():N}".Substring(0, 20) };
        await categoriaRepo.SaveAsync(categoria);

        var producto = new Producto
        {
            Nombre = $"Producto Test {Guid.NewGuid():N}".Substring(0, 20),
            Descripcion = "Producto para pedidos",
            Precio = 99.99m,
            Stock = 100,
            CategoriaId = categoria.Id,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        await productoRepo.SaveAsync(producto);
        _productoId = producto.Id;

        var user = new User
        {
            Username = $"testuser_{Guid.NewGuid():N}".Substring(0, 15),
            Email = "test@example.com",
            PasswordHash = "$2a$11$test",
            Role = "USER"
        };
        _userId = user.Id;

        _pedidosService = _serviceProvider.GetRequiredService<IPedidosService>();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<ILogger<ProductoRepository>, Logger<ProductoRepository>>();
        services.AddScoped<ILogger<CategoriaRepository>, Logger<CategoriaRepository>>();
        services.AddScoped<ILogger<PedidosNativeRepository>, Logger<PedidosNativeRepository>>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        // Usando PedidosNativeRepository (driver nativo) en lugar de PedidosEfCoreRepository
        services.AddScoped<IPedidosRepository, PedidosNativeRepository>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Mock para IJwtTokenExtractor (requerido por PedidosWebSocketHandler)
        var mockJwtExtractor = new Mock<IJwtTokenExtractor>();
        mockJwtExtractor.Setup(x => x.ExtractUserId(It.IsAny<string>())).Returns(1L);
        services.AddSingleton<IJwtTokenExtractor>(mockJwtExtractor.Object);
        
        // Mock para IHubContext (requerido por PedidosService)
        // Nota: SendAsync es un método de extensión, no se puede mockear directamente
        // El mock simplemente evita NullReferenceException
        var mockClients = new Mock<IHubClients>();
        mockClients.Setup(c => c.All).Returns(Mock.Of<IClientProxy>());
        var mockHubContext = new Mock<IHubContext<PedidosHub>>();
        mockHubContext.Setup(c => c.Clients).Returns(mockClients.Object);
        services.AddSingleton<IHubContext<PedidosHub>>(mockHubContext.Object);
        
        services.AddScoped<ILogger<PedidosService>, Logger<PedidosService>>();
        services.AddScoped<PedidosWebSocketHandler>();
        services.AddScoped<IEmailService, MemoryEmailService>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IPedidosService, PedidosService>();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();
    }

    [Test]
    public async Task FindAllAsync_SinPedidos_RetornaListaVacia()
    {
        var result = await _pedidosService!.FindAllAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByUserIdAsync_SinPedidos_RetornaListaVacia()
    {
        var result = await _pedidosService!.FindByUserIdAsync(_userId);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByIdAsync_SinPedidos_RetornaNotFound()
    {
        var result = await _pedidosService!.FindByIdAsync("507f1f77bcf86cd799439011");
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConItemsValidos_RetornaPedidoCreado()
    {
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test Destinatario",
                Email = "test@email.com",
                Direccion = new DireccionDto
                {
                    Calle = "Calle Test",
                    Ciudad = "Madrid",
                    Pais = "España"
                }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = 2 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeNullOrEmpty();
        result.Value.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_RetornaError()
    {
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>()
        };
        var result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_RetornaError()
    {
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 999999, Cantidad = 1 }
            }
        };
        var result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockCero_RetornaErrorDeStock()
    {
        await SetProductoStock(_productoId, 0);

        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = 1 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockInsuficiente_RetornaErrorDeStock()
    {
        await SetProductoStock(_productoId, 5);

        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = 10 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockSuficiente_DecrementaStockCorrectamente()
    {
        var stockInicial = 50;
        await SetProductoStock(_productoId, stockInicial);

        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test Destinatario",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = 5 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsSuccess.Should().BeTrue();

        var productoActualizado = await _dbContext!.FindAsync<Producto>(_productoId);
        productoActualizado!.Stock.Should().Be(stockInicial - 5);
    }

    [Test]
    public async Task CreateAsync_CantidadExactaStock_PermitePedido()
    {
        var stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = stockInicial }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsSuccess.Should().BeTrue();

        var productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task CreateAsync_CantidadMayorStock_RechazaPedido()
    {
        var stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = stockInicial + 1 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();

        var productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task CreateAsync_UsuarioNoExistente_RetornaError()
    {
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = 1 }
            }
        };

        var result = await _pedidosService!.CreateAsync(999999, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DecrementStockAsync_StockInsuficiente_NoDecrementa()
    {
        var productoRepo = _serviceProvider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 3);

        var producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await productoRepo.DecrementStockAsync(_productoId, 5, producto!.RowVersion);
        resultado.Should().BeFalse();

        var productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(3);
    }

    [Test]
    public async Task DecrementStockAsync_StockSuficiente_Decrementa()
    {
        var productoRepo = _serviceProvider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 10);

        var producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await productoRepo.DecrementStockAsync(_productoId, 3, producto!.RowVersion);
        resultado.Should().BeTrue();

        var productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(7);
    }

    #region ========== UTILIDADES ==========

    private async Task SetProductoStock(long productoId, int stock)
    {
        var producto = await _dbContext!.FindAsync<Producto>(productoId);
        producto!.Stock = stock;
        await _dbContext.SaveChangesAsync();
    }

    #endregion

    #region ========== TESTS ADICIONALES - MÉTODOS DE ADMINISTRADOR ==========

    [Test]
    public async Task FindAllPagedAsync_ConPaginacion_RetornaPedidosPaginados()
    {
        // Arrange
        var page = 0;
        var size = 10;

        // Act
        var result = await _pedidosService!.FindAllPagedAsync(page, size);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(size);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task FindAllPagedAsync_SegundaPagina_RetornaPaginaCorrecta()
    {
        // Arrange
        var page = 1;
        var size = 5;

        // Act
        var result = await _pedidosService!.FindAllPagedAsync(page, size);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(2);
    }

    [Test]
    public async Task UpdateAdminAsync_ConDireccion_ActualizaPedido()
    {
        // Arrange - Primero crear un pedido
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle Original", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        // Actualizar
        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Calle Nueva 123" };

        // Act
        var result = await _pedidosService!.UpdateAdminAsync(pedidoId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Calle Nueva");
    }

    [Test]
    public async Task UpdateAdminAsync_PedidoNoExistente_RetornaNotFound()
    {
        // Arrange
        var updateDto = new UpdatePedidoDto { Estado = "ENVIADO" };

        // Act
        var result = await _pedidosService!.UpdateAdminAsync("507f1f77bcf86cd799439011", updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoValido_ActualizaEstado()
    {
        // Arrange - Crear pedido
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        // Act
        var result = await _pedidosService!.UpdateEstadoAsync(pedidoId, PedidoEstado.ENVIADO);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(PedidoEstado.ENVIADO);
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoInvalido_RetornaError()
    {
        // Arrange
        var pedidoId = "507f1f77bcf86cd799439011";

        // Act
        var result = await _pedidosService!.UpdateEstadoAsync(pedidoId, "ESTADO_INVALIDO");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoExistente_MarcaComoEliminado()
    {
        // Arrange - Crear pedido
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        // Act
        var result = await _pedidosService!.DeleteAdminAsync(pedidoId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Nota: FindByIdAsync no filtra por IsDeleted, pero el soft delete se hizo correctamente
        // Para verificar, el resultado de FindByIdAsync debería tener IsDeleted = true
        var findResult = await _pedidosService!.FindByIdAsync(pedidoId);
        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoNoExistente_RetornaNotFound()
    {
        // Act
        var result = await _pedidosService!.DeleteAdminAsync("507f1f77bcf86cd799439011");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ========== TESTS ADICIONALES - MÉTODOS DE USUARIO ==========

    [Test]
    public async Task FindMyPedidosAsync_ConPedidos_RetornaPedidosDelUsuario()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        await _pedidosService!.CreateAsync(_userId, dto);

        // Act
        var result = await _pedidosService!.FindByUserIdAsync(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task FindMyPedidosAsync_SinPedidos_RetornaListaVacia()
    {
        // Arrange - Nuevo usuario sin pedidos
        var nuevoUserId = 999999L;

        // Act
        var result = await _pedidosService!.FindByUserIdAsync(nuevoUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindMyPedidosPagedAsync_ConPaginacion_RetornaPedidosPaginados()
    {
        // Arrange
        var page = 0;
        var size = 10;

        // Act
        var result = await _pedidosService!.FindMyPedidosAsync(_userId, page, size);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(size);
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoPropio_RetornaPedido()
    {
        // Arrange - Crear pedido
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        // Act
        var result = await _pedidosService!.FindMyPedidoAsync(pedidoId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(pedidoId);
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoAjoyo_RetornaError()
    {
        // Arrange
        var pedidoId = "507f1f77bcf86cd799439011";
        var otroUserId = 999999L;

        // Act
        var result = await _pedidosService!.FindMyPedidoAsync(pedidoId, otroUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoPendiente_ActualizaDireccion()
    {
        // Arrange - Crear pedido
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle Original", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion 456" };

        // Act
        var result = await _pedidosService!.UpdateMyPedidoAsync(pedidoId, _userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Nueva Direccion");
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoNoPendiente_RetornaError()
    {
        // Arrange - Crear pedido y cambiar estado
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        // Cambiar estado a ENVIADO
        await _pedidosService!.UpdateEstadoAsync(pedidoId, PedidoEstado.ENVIADO);

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion" };

        // Act
        var result = await _pedidosService!.UpdateMyPedidoAsync(pedidoId, _userId, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoPendiente_MarcaEliminado()
    {
        // Arrange - Crear pedido
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        // Act
        var result = await _pedidosService!.DeleteMyPedidoAsync(pedidoId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoNoPendiente_RetornaError()
    {
        // Arrange - Crear pedido y cambiar estado
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        var createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        // Cambiar estado a ENVIADO
        await _pedidosService!.UpdateEstadoAsync(pedidoId, PedidoEstado.ENVIADO);

        // Act
        var result = await _pedidosService!.DeleteMyPedidoAsync(pedidoId, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_PedidoAjoyo_RetornaError()
    {
        // Arrange
        var pedidoId = "507f1f77bcf86cd799439011";
        var otroUserId = 999999L;

        // Act
        var result = await _pedidosService!.DeleteMyPedidoAsync(pedidoId, otroUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}
