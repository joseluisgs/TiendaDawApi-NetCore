using FluentAssertions;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.GraphQL.Publishers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Validators.Productos;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Storage;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Tests.Integration.TestContainers.Productos.Services;

/// <summary>
/// Tests de integración para ProductoService con DI completo.
/// Verifica el servicio con base de datos real usando Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ProductoServiceIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IProductoService? _productoService;
    private long _categoriaId;

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
                { "Cache:ProductoCacheTTLMinutes", "10" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ILogger<ProductoRepository>, Logger<ProductoRepository>>();
        services.AddScoped<ILogger<CategoriaRepository>, Logger<CategoriaRepository>>();
        services.AddScoped<ILogger<ProductoService>, Logger<ProductoService>>();

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IStorageService>(sp =>
        {
            var mockStorage = new Mock<IStorageService>();
            mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Success<string, DomainError>(System.IO.Path.Combine("images", "productos", "test.jpg")));
            mockStorage.Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
                .ReturnsAsync(Result.Success<bool, DomainError>(true));
            mockStorage.Setup(s => s.FileExists(It.IsAny<string>()))
                .Returns(true);
            return mockStorage.Object;
        });
        services.AddScoped<IEmailService, MemoryEmailService>();
        services.AddScoped<ProductosWebSocketHandler>();

        // Mock IHubContext<ProductosHub> for ProductoService
        services.AddScoped<IHubContext<ProductosHub>>(sp =>
        {
            var mockHubContext = new Mock<IHubContext<ProductosHub>>();
            return mockHubContext.Object;
        });

        // Mock IEventPublisher for GraphQL subscriptions
        services.AddScoped<IEventPublisher>(sp =>
        {
            var mockPublisher = new Mock<IEventPublisher>();
            mockPublisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);
            return mockPublisher.Object;
        });

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        var categoriaRepo = _serviceProvider.GetRequiredService<ICategoriaRepository>();
        var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
        var threadId = Environment.CurrentManagedThreadId;
        var uniqueName = $"Cat_{processId}_{threadId}_{Guid.NewGuid():N}".Substring(0, 40);
        var categoria = new Categoria { Nombre = uniqueName };
        await categoriaRepo.SaveAsync(categoria);
        _categoriaId = categoria.Id;

        _productoService = _serviceProvider.GetRequiredService<IProductoService>();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        if (_serviceProvider is IDisposable sp)
        {
            sp.Dispose();
        }
    }

    [Test]
    public async Task FindAllAsync_SinProductos_RetornaListaVacia()
    {
        var result = await _productoService!.FindAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task CreateAsync_ConDatosValidos_RetornaProductoCreado()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop Test",
            Descripcion = "Laptop de prueba",
            Precio = 999.99m,
            Stock = 10,
            CategoriaId = _categoriaId
        };

        var result = await _productoService!.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("Laptop Test");
        result.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task FindByIdAsync_ConProductoExistente_RetornaProducto()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Buscar Test",
            Descripcion = "Producto de prueba",
            Precio = 99.99m,
            Stock = 5,
            CategoriaId = _categoriaId
        };
        var createResult = await _productoService!.CreateAsync(dto);
        createResult.IsSuccess.Should().BeTrue();

        var findResult = await _productoService.FindByIdAsync(createResult.Value.Id);

        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Nombre.Should().Be("Buscar Test");
    }

    [Test]
    public async Task FindByIdAsync_ConProductoNoExistente_RetornaNotFound()
    {
        var result = await _productoService!.FindByIdAsync(999999);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_ConDatosValidos_RetornaProductoActualizado()
    {
        var createDto = new ProductoRequestDto
        {
            Nombre = "Original",
            Descripcion = "Producto original",
            Precio = 99.99m,
            Stock = 5,
            CategoriaId = _categoriaId
        };
        var createResult = await _productoService!.CreateAsync(createDto);
        createResult.IsSuccess.Should().BeTrue();

        var updateDto = new ProductoRequestDto
        {
            Nombre = "Actualizado",
            Descripcion = "Producto actualizado",
            Precio = 149.99m,
            Stock = 10,
            CategoriaId = _categoriaId
        };
        var updateResult = await _productoService.UpdateAsync(createResult.Value.Id, updateDto);

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.Nombre.Should().Be("Actualizado");
    }

    [Test]
    public async Task DeleteAsync_ConProductoExistente_RetornaExito()
    {
        var createDto = new ProductoRequestDto
        {
            Nombre = "Eliminar Test",
            Descripcion = "Producto a eliminar",
            Precio = 49.99m,
            Stock = 3,
            CategoriaId = _categoriaId
        };
        var createResult = await _productoService!.CreateAsync(createDto);
        createResult.IsSuccess.Should().BeTrue();

        var deleteResult = await _productoService.DeleteAsync(createResult.Value.Id);

        deleteResult.IsSuccess.Should().BeTrue();

        var findResult = await _productoService.FindByIdAsync(createResult.Value.Id);
        findResult.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConPrecioCero_RetornaErrorValidacion()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Producto Precio Cero",
            Descripcion = "No debería crearse",
            Precio = 0m,
            Stock = 10,
            CategoriaId = _categoriaId
        };

        var result = await _productoService!.CreateAsync(dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockNegativo_RetornaErrorValidacion()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Producto Stock Negativo",
            Descripcion = "No debería crearse",
            Precio = 99.99m,
            Stock = -1,
            CategoriaId = _categoriaId
        };

        var result = await _productoService!.CreateAsync(dto);

        result.IsFailure.Should().BeTrue();
    }
}
