using System.Diagnostics;
using System.Threading.Channels;
using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Pedidos;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
///     Tests de integración para PedidosService con DI completo.
///     Verifica el servicio con bases de datos reales usando Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class PedidosServiceIntegrationTests {
    [OneTimeSetUp]
    public async Task OneTimeSetup() {
        _connectionString = await AssemblyContainerFixture.CreatePostgresDatabaseAsync(DatabaseName);
        _mongoConnectionString = AssemblyContainerFixture.MongoConnectionString;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await AssemblyContainerFixture.DropPostgresDatabaseAsync(DatabaseName);
        AssemblyContainerFixture.DropMongoDatabase(DatabaseName);
    }

    [SetUp]
    public async Task Setup() {
        string connectionString = _connectionString;
        string mongoConnectionString = _mongoConnectionString;

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", connectionString },
                { "MongoDbSettings:ConnectionString", mongoConnectionString },
                { "MongoDbSettings:DatabaseName", DatabaseName },
                { "Cache:PedidoCacheTTLMinutes", "5" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        services.AddSingleton(ActivitySource);

        services.AddLogging(builder => {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContext<TiendaMongoContext>(options =>
            options.UseMongoDB(mongoConnectionString, DatabaseName));

        RegisterRepositories(services, mongoConnectionString);
        RegisterServices(services);

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        IProductoRepository productoRepo = _serviceProvider.GetRequiredService<IProductoRepository>();
        ICategoriaRepository categoriaRepo = _serviceProvider.GetRequiredService<ICategoriaRepository>();
        var categoria = new Categoria { Nombre = $"Test Categoria {Guid.NewGuid():N}".Substring(0, 20) };
        await categoriaRepo.SaveAsync(categoria);

        var producto = new Producto {
            Nombre = $"Producto Test {Guid.NewGuid():N}".Substring(0, 20),
            Descripcion = "Producto para pedidos",
            Precio = 99.99m,
            Stock = 100,
            CategoriaId = categoria.Id,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        await productoRepo.SaveAsync(producto);
        _productoId = producto.Id;

        var user = new User {
            UserName = $"testuser_{Guid.NewGuid():N}".Substring(0, 15),
            Email = "test@example.com",
            PasswordHash = "$2a$11$test",
            Role = "USER"
        };
        _userId = user.Id;

        _pedidosService = _serviceProvider.GetRequiredService<IPedidosService>();
    }

    [TearDown]
    public void TearDown() {
        _dbContext?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }

    private static readonly ActivitySource ActivitySource = new("PedidosServiceIntegrationTests");
    private const string DatabaseName = "it_pedidos_service";
    private string _connectionString = string.Empty;
    private string _mongoConnectionString = string.Empty;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IPedidosService? _pedidosService;
    private long _productoId;
    private long _userId;

    private static void RegisterRepositories(IServiceCollection services, string mongoConnectionString) {
        services.AddScoped<ILogger<ProductoRepository>, Logger<ProductoRepository>>();
        services.AddScoped<ILogger<CategoriaRepository>, Logger<CategoriaRepository>>();
        services.AddScoped<ILogger<PedidosEfCoreRepository>, Logger<PedidosEfCoreRepository>>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IPedidosRepository, PedidosEfCoreRepository>();
    }

    private static void RegisterServices(IServiceCollection services) {
        // Mock para IJwtTokenExtractor (requerido por PedidosWebSocketHandler)
        var mockJwtExtractor = new Mock<IJwtTokenExtractor>();
        mockJwtExtractor.Setup(x => x.ExtractUserId(It.IsAny<string>())).Returns(1L);
        services.AddSingleton(mockJwtExtractor.Object);

        // Mock para IHubContext (requerido por PedidosService)
        var mockClients = new Mock<IHubClients>();
        mockClients.Setup(c => c.All).Returns(Mock.Of<IClientProxy>());
        var mockHubContext = new Mock<IHubContext<PedidosHub>>();
        mockHubContext.Setup(c => c.Clients).Returns(mockClients.Object);
        services.AddSingleton(mockHubContext.Object);

        services.AddScoped<ILogger<PedidosService>, Logger<PedidosService>>();
        services.AddScoped<PedidosWebSocketHandler>();
        // El handler valida el JWT con IJwtService al aceptar la conexión:
        // el contenedor de este test debe registrarlo igual que ServicesConfig.
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IEmailService, MemoryEmailService>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddOutputCache();
        services.AddScoped<IPedidoEffects, PedidoEffects>();
        services.AddScoped<IPedidosService, PedidosService>();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();
    }

    [Test]
    public async Task FindAllAsync_SinPedidos_RetornaListaVacia() {
        Result<IEnumerable<PedidoDto>, DomainError> result = await _pedidosService!.FindAllAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByUserIdAsync_SinPedidos_RetornaListaVacia() {
        Result<IEnumerable<PedidoDto>, DomainError> result = await _pedidosService!.FindByUserIdAsync(_userId);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByIdAsync_SinPedidos_RetornaNotFound() {
        Result<PedidoDto, DomainError> result = await _pedidosService!.FindByIdAsync("507f1f77bcf86cd799439011");
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConItemsValidos_RetornaPedidoCreado() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test Destinatario",
                Email = "test@email.com",
                Direccion = new DireccionDto {
                    Calle = "Calle Test",
                    Ciudad = "Madrid",
                    Pais = "España"
                }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = _productoId, Cantidad = 2 }
            }
        };

        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeNullOrEmpty();
        result.Value.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_RetornaError() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>()
        };
        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_RetornaError() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = 999999, Cantidad = 1 }
            }
        };
        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockCero_RetornaErrorDeStock() {
        await SetProductoStock(_productoId, 0);

        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = _productoId, Cantidad = 1 }
            }
        };

        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockInsuficiente_RetornaErrorDeStock() {
        await SetProductoStock(_productoId, 5);

        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = _productoId, Cantidad = 10 }
            }
        };

        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockSuficiente_DecrementaStockCorrectamente() {
        int stockInicial = 50;
        await SetProductoStock(_productoId, stockInicial);

        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test Destinatario",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = _productoId, Cantidad = 5 }
            }
        };

        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsSuccess.Should().BeTrue();

        Producto? productoActualizado = await _dbContext!.FindAsync<Producto>(_productoId);
        productoActualizado!.Stock.Should().Be(stockInicial - 5);
    }

    [Test]
    public async Task CreateAsync_CantidadExactaStock_PermitePedido() {
        int stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = _productoId, Cantidad = stockInicial }
            }
        };

        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsSuccess.Should().BeTrue();

        Producto? productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task CreateAsync_CantidadMayorStock_RechazaPedido() {
        int stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = _productoId, Cantidad = stockInicial + 1 }
            }
        };

        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(_userId, dto);
        result.IsFailure.Should().BeTrue();

        Producto? productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task CreateAsync_UsuarioNoExistente_RetornaError() {
        var dto = new PedidoRequestDto {
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = _productoId, Cantidad = 1 }
            }
        };

        Result<PedidoDto, DomainError> result = await _pedidosService!.CreateAsync(999999, dto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DecrementStockAsync_StockInsuficiente_NoDecrementa() {
        IProductoRepository productoRepo = _serviceProvider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 3);

        Producto? producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await productoRepo.DecrementStockAsync(_productoId, 5, producto!.RowVersion);
        resultado.Should().BeFalse();

        Producto? productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(3);
    }

    [Test]
    public async Task DecrementStockAsync_StockSuficiente_Decrementa() {
        IProductoRepository productoRepo = _serviceProvider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 10);

        Producto? producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await productoRepo.DecrementStockAsync(_productoId, 3, producto!.RowVersion);
        resultado.Should().BeTrue();

        Producto? productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(7);
    }

    [Test]
    public async Task FindAllPagedAsync_ConPaginacion_RetornaPedidosPaginados() {
        int page = 0;
        int size = 10;
        Result<PagedResult<PedidoDto>, DomainError> result = await _pedidosService!.FindAllPagedAsync(page, size);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
    }

    [Test]
    public async Task FindAllPagedAsync_SegundaPagina_RetornaPaginaCorrecta() {
        int page = 1;
        int size = 5;
        Result<PagedResult<PedidoDto>, DomainError> result = await _pedidosService!.FindAllPagedAsync(page, size);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(2);
    }

    [Test]
    public async Task UpdateAdminAsync_ConDireccion_ActualizaPedido() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle Original", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Calle Nueva 123" };
        Result<PedidoDto, DomainError> result = await _pedidosService!.UpdateAdminAsync(pedidoId, updateDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Calle Nueva");
    }

    [Test]
    public async Task UpdateAdminAsync_PedidoNoExistente_RetornaNotFound() {
        var updateDto = new UpdatePedidoDto { Estado = "ENVIADO" };
        Result<PedidoDto, DomainError> result =
            await _pedidosService!.UpdateAdminAsync("507f1f77bcf86cd799439011", updateDto);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoValido_ActualizaEstado() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        Result<PedidoDto, DomainError>
            result = await _pedidosService!.UpdateEstadoAsync(pedidoId, PedidoEstado.ENVIADO);
        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(PedidoEstado.ENVIADO);
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoInvalido_RetornaError() {
        string pedidoId = "507f1f77bcf86cd799439011";
        Result<PedidoDto, DomainError> result = await _pedidosService!.UpdateEstadoAsync(pedidoId, "ESTADO_INVALIDO");
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoExistente_MarcaComoEliminado() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        UnitResult<DomainError> result = await _pedidosService!.DeleteAdminAsync(pedidoId);
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoNoExistente_RetornaNotFound() {
        UnitResult<DomainError> result = await _pedidosService!.DeleteAdminAsync("507f1f77bcf86cd799439011");
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task FindMyPedidosAsync_ConPedidos_RetornaPedidosDelUsuario() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        await _pedidosService!.CreateAsync(_userId, dto);
        Result<IEnumerable<PedidoDto>, DomainError> result = await _pedidosService!.FindByUserIdAsync(_userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task FindMyPedidosAsync_SinPedidos_RetornaListaVacia() {
        long nuevoUserId = 999999L;
        Result<IEnumerable<PedidoDto>, DomainError> result = await _pedidosService!.FindByUserIdAsync(nuevoUserId);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindMyPedidosPagedAsync_ConPaginacion_RetornaPedidosPaginados() {
        int page = 0;
        int size = 10;
        Result<PagedResult<PedidoDto>, DomainError> result =
            await _pedidosService!.FindMyPedidosAsync(_userId, page, size);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoPropio_RetornaPedido() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        Result<PedidoDto, DomainError> result = await _pedidosService!.FindMyPedidoAsync(pedidoId, _userId);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoAjeno_RetornaError() {
        string pedidoId = "507f1f77bcf86cd799439011";
        long otroUserId = 999999L;
        Result<PedidoDto, DomainError> result = await _pedidosService!.FindMyPedidoAsync(pedidoId, otroUserId);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoPendiente_ActualizaDireccion() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle Original", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion 456" };
        Result<PedidoDto, DomainError>
            result = await _pedidosService!.UpdateMyPedidoAsync(pedidoId, _userId, updateDto);

        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Nueva Direccion");
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoNoPendiente_RetornaError() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        await _pedidosService!.UpdateEstadoAsync(pedidoId, PedidoEstado.ENVIADO);

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion" };
        Result<PedidoDto, DomainError>
            result = await _pedidosService!.UpdateMyPedidoAsync(pedidoId, _userId, updateDto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoPendiente_MarcaEliminado() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        UnitResult<DomainError> result = await _pedidosService!.DeleteMyPedidoAsync(pedidoId, _userId);
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoNoPendiente_RetornaError() {
        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = _productoId, Cantidad = 1 } }
        };

        Result<PedidoDto, DomainError> createResult = await _pedidosService!.CreateAsync(_userId, dto);
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        await _pedidosService!.UpdateEstadoAsync(pedidoId, PedidoEstado.ENVIADO);

        UnitResult<DomainError> result = await _pedidosService!.DeleteMyPedidoAsync(pedidoId, _userId);
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_PedidoAjeno_RetornaError() {
        string pedidoId = "507f1f77bcf86cd799439011";
        long otroUserId = 999999L;
        UnitResult<DomainError> result = await _pedidosService!.DeleteMyPedidoAsync(pedidoId, otroUserId);
        result.IsFailure.Should().BeTrue();
    }

    private async Task SetProductoStock(long productoId, int stock) {
        Producto? producto = await _dbContext!.FindAsync<Producto>(productoId);
        producto!.Stock = stock;
        await _dbContext.SaveChangesAsync();
    }
}
