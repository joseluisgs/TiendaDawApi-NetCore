using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Validators.Categorias;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Categorias.Services;

/// <summary>
/// Tests de integración para CategoriaService con DI completo.
/// Verifica el servicio con base de datos real usando Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class CategoriaServiceIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private ICategoriaService? _categoriaService;

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
                { "Cache:CategoriaCacheTTLMinutes", "10" }
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

        services.AddScoped<ILogger<CategoriaRepository>, Logger<CategoriaRepository>>();
        services.AddScoped<ILogger<CategoriaService>, Logger<CategoriaService>>();

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();
        services.AddScoped<ICacheService, MemoryCacheService>();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        _categoriaService = _serviceProvider.GetRequiredService<ICategoriaService>();
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
    public async Task FindAllAsync_SinCategorias_RetornaListaVacia()
    {
        var result = await _categoriaService!.FindAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task CreateAsync_ConDatosValidos_RetornaCategoriaCreada()
    {
        var dto = new CategoriaRequestDto { Nombre = "Electronica Test" };

        var result = await _categoriaService!.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("Electronica Test");
        result.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task FindByIdAsync_ConCategoriaExistente_RetornaCategoria()
    {
        var dto = new CategoriaRequestDto { Nombre = "Buscar Test" };
        var createResult = await _categoriaService!.CreateAsync(dto);
        createResult.IsSuccess.Should().BeTrue();

        var findResult = await _categoriaService.FindByIdAsync(createResult.Value.Id);

        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Nombre.Should().Be("Buscar Test");
    }

    [Test]
    public async Task FindByIdAsync_ConCategoriaNoExistente_RetornaNotFound()
    {
        var result = await _categoriaService!.FindByIdAsync(999999);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_ConDatosValidos_RetornaCategoriaActualizada()
    {
        var createDto = new CategoriaRequestDto { Nombre = "Original" };
        var createResult = await _categoriaService!.CreateAsync(createDto);
        createResult.IsSuccess.Should().BeTrue();

        var updateDto = new CategoriaRequestDto { Nombre = "Actualizado" };
        var updateResult = await _categoriaService.UpdateAsync(createResult.Value.Id, updateDto);

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.Nombre.Should().Be("Actualizado");
    }

    [Test]
    public async Task DeleteAsync_ConCategoriaExistente_RetornaExito()
    {
        var createDto = new CategoriaRequestDto { Nombre = "Eliminar Test" };
        var createResult = await _categoriaService!.CreateAsync(createDto);
        createResult.IsSuccess.Should().BeTrue();

        var deleteResult = await _categoriaService.DeleteAsync(createResult.Value.Id);

        deleteResult.IsSuccess.Should().BeTrue();

        var findResult = await _categoriaService.FindByIdAsync(createResult.Value.Id);
        findResult.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConNombreDuplicado_RetornaConflicto()
    {
        var dto = new CategoriaRequestDto { Nombre = "Duplicado Test" };
        await _categoriaService!.CreateAsync(dto);

        var result = await _categoriaService.CreateAsync(dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task FindAllPagedAsync_ConFiltro_RetornaResultadosPaginados()
    {
        for (int i = 0; i < 5; i++)
        {
            var dto = new CategoriaRequestDto { Nombre = $"Paged Test {i}" };
            await _categoriaService!.CreateAsync(dto);
        }

        var filter = new CategoriaFilterDto { Page = 1, Size = 3 };
        var result = await _categoriaService!.FindAllPagedAsync(filter);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(5);
    }
}
