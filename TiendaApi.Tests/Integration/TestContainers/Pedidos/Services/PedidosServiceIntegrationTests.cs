using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Pedidos;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Validators.Pedidos;
using TiendaApi.Apis.WebSockets.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
/// Tests de integración para PedidosService con DI completo.
/// Verifica el servicio con bases de datos reales usando Testcontainers.
/// </summary>
[TestFixture]
[NonParallelizable]
[Ignore("Se necesita librería MongoDB.EntityFrameworkCore compatible con EF Core 10 - bug EF-272")]
public class PedidosServiceIntegrationTests
{
    private static readonly ActivitySource ActivitySource = new("PedidosServiceIntegrationTests");
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

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", connectionString },
                { "MongoDbSettings:ConnectionString", mongoConnectionString },
                { "MongoDbSettings:DatabaseName", "tienda_test" },
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

        services.AddDbContext<TiendaMongoContext>(options =>
            options.UseMongoDB(mongoConnectionString, "tienda_test"));

        RegisterRepositories(services, mongoConnectionString);
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

    private static void RegisterRepositories(IServiceCollection services, string mongoConnectionString)
    {
        services.AddScoped<ILogger<ProductoRepository>, Logger<ProductoRepository>>();
        services.AddScoped<ILogger<CategoriaRepository>, Logger<CategoriaRepository>>();
        services.AddScoped<ILogger<PedidosRepository>, Logger<PedidosRepository>>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IPedidosRepository, PedidosRepository>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ILogger<PedidosService>, Logger<PedidosService>>();
        services.AddScoped<PedidoWebSocketHandler>();
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
    [Ignore("Se necesita librería MongoDB.EntityFrameworkCore compatible con EF Core 10 - bug EF-272")]
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
    [Ignore("Se necesita librería MongoDB.EntityFrameworkCore compatible con EF Core 10 - bug EF-272")]
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
    [Ignore("Se necesita librería MongoDB.EntityFrameworkCore compatible con EF Core 10 - bug EF-272")]
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
    [Ignore("Se necesita librería MongoDB.EntityFrameworkCore compatible con EF Core 10 - bug EF-272")]
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
    [Ignore("Se necesita librería MongoDB.EntityFrameworkCore compatible con EF Core 10 - bug EF-272")]
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

    private async Task SetProductoStock(long productoId, int stock)
    {
        var producto = await _dbContext!.FindAsync<Producto>(productoId);
        producto!.Stock = stock;
        await _dbContext.SaveChangesAsync();
    }
}
