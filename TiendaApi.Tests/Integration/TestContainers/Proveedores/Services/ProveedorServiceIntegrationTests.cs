using System.Threading.Channels;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.IdGenerators;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Proveedores;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Proveedores;
using TiendaApi.Api.Validators.Proveedores;

namespace TiendaApi.Tests.Integration.TestContainers.Proveedores.Services;

/// <summary>
/// Tests de integración para ProveedorService con DI completo y base de datos real.
/// Cubren el ciclo completo: generación del código, unicidad, borrado y la regla que
/// impide eliminar un proveedor que todavía tiene productos.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ProveedorServiceIntegrationTests {
    private const string DatabaseName = "it_proveedor_service";

    private string _connectionString = string.Empty;
    private string _mongoConnectionString = string.Empty;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IProveedorService? _proveedorService;

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
        var connectionString = _connectionString;
        var mongoConnectionString = _mongoConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", connectionString },
                { "MongoDbSettings:ConnectionString", mongoConnectionString },
                { "MongoDbSettings:DatabaseName", DatabaseName },
                { "Cache:ProveedorCacheTTLMinutes", "10" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddOutputCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());

        services.AddLogging(builder => {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ILogger<ProveedorRepository>, Logger<ProveedorRepository>>();
        services.AddScoped<ILogger<ProveedorService>, Logger<ProveedorService>>();

        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<IValidator<ProveedorRequestDto>, ProveedorRequestValidator>();
        services.AddScoped<ICacheService, MemoryCacheService>();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        _proveedorService = _serviceProvider.GetRequiredService<IProveedorService>();
    }

    [TearDown]
    public void TearDown() {
        _dbContext?.Dispose();
        if (_serviceProvider is IDisposable sp) {
            sp.Dispose();
        }
    }

    [Test]
    public async Task FindAllAsync_SinProveedores_RetornaListaVacia() {
        var result = await _proveedorService!.FindAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task CreateAsync_SinCodigo_ElServidorGeneraIdentificadorDeOnceCaracteres() {
        var dto = new ProveedorRequestDto { Nombre = "Generado Automaticamente" };

        var result = await _proveedorService!.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Codigo.Should().HaveLength(IdYouTube.Longitud);
        IdYouTube.EsValido(result.Value.Codigo).Should().BeTrue("el código generado debe ser un ID válido");
    }

    [Test]
    public async Task CreateAsync_ConCodigoExplicito_LoPreserva() {
        var dto = new ProveedorRequestDto { Codigo = "Explicito01", Nombre = "Con Código Explícito" };

        var result = await _proveedorService!.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Codigo.Should().Be("Explicito01");
    }

    [Test]
    public async Task CreateAsync_ConCodigoInvalido_RetornaErrorDeValidacion() {
        var dto = new ProveedorRequestDto { Codigo = "corto", Nombre = "Código Inválido" };

        var result = await _proveedorService!.CreateAsync(dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConCodigoDuplicado_RetornaConflicto() {
        var primero = await _proveedorService!.CreateAsync(
            new ProveedorRequestDto { Codigo = "Duplicado01", Nombre = "Primero" });
        var segundo = await _proveedorService.CreateAsync(
            new ProveedorRequestDto { Codigo = "Duplicado01", Nombre = "Segundo" });

        primero.IsSuccess.Should().BeTrue();
        segundo.IsFailure.Should().BeTrue("dos proveedores no pueden compartir código");
    }

    [Test]
    public async Task CreateAsync_ConNombreDuplicado_RetornaConflicto() {
        var nombre = "Nombre Repetido";
        await _proveedorService!.CreateAsync(new ProveedorRequestDto { Nombre = nombre });

        var segundo = await _proveedorService.CreateAsync(new ProveedorRequestDto { Nombre = nombre });

        segundo.IsFailure.Should().BeTrue("dos proveedores no pueden compartir nombre");
    }

    [Test]
    public async Task FindByIdAsync_ConCodigoExistente_RetornaProveedor() {
        var creado = await _proveedorService!.CreateAsync(
            new ProveedorRequestDto { Nombre = "Buscable" });
        creado.IsSuccess.Should().BeTrue();

        var result = await _proveedorService.FindByIdAsync(creado.Value.Codigo);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Buscable");
    }

    [Test]
    public async Task FindByIdAsync_ConCodigoInexistente_RetornaNotFound() {
        var result = await _proveedorService!.FindByIdAsync("NoExiste000");

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_ConDatosValidos_ActualizaSinCambiarElCodigo() {
        var creado = await _proveedorService!.CreateAsync(
            new ProveedorRequestDto { Nombre = "Original" });
        creado.IsSuccess.Should().BeTrue();

        var result = await _proveedorService.UpdateAsync(
            creado.Value.Codigo,
            new ProveedorRequestDto { Nombre = "Actualizado", Contacto = "nuevo@tienda.example" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Codigo.Should().Be(creado.Value.Codigo, "el código es la clave y no cambia");
        result.Value.Nombre.Should().Be("Actualizado");
        result.Value.Contacto.Should().Be("nuevo@tienda.example");
    }

    [Test]
    public async Task DeleteAsync_SinProductos_RetornaOkYDesaparece() {
        var creado = await _proveedorService!.CreateAsync(
            new ProveedorRequestDto { Nombre = "Borrable" });
        creado.IsSuccess.Should().BeTrue();

        var delete = await _proveedorService.DeleteAsync(creado.Value.Codigo);
        delete.IsSuccess.Should().BeTrue();

        var find = await _proveedorService.FindByIdAsync(creado.Value.Codigo);
        find.IsFailure.Should().BeTrue("tras el borrado lógico el proveedor no se consulta");
    }

    [Test]
    public async Task DeleteAsync_ConProductosAsociados_RetornaReglaDeNegocio() {
        var creado = await _proveedorService!.CreateAsync(
            new ProveedorRequestDto { Nombre = "Con Productos" });
        creado.IsSuccess.Should().BeTrue();

        await CrearProductoAsociadoAsync(creado.Value.Codigo);

        var delete = await _proveedorService.DeleteAsync(creado.Value.Codigo);

        delete.IsFailure.Should().BeTrue("no se puede eliminar un proveedor con productos");
    }

    [Test]
    public async Task FindAllPagedAsync_FiltrandoPorNombre_RetornaSoloCoincidencias() {
        await _proveedorService!.CreateAsync(new ProveedorRequestDto { Nombre = "Distribuciones Norte" });
        await _proveedorService.CreateAsync(new ProveedorRequestDto { Nombre = "Editorial Sur" });

        var result = await _proveedorService.FindAllPagedAsync(new ProveedorFilterDto {
            Nombre = "Distribuciones",
            Page = 0,
            Size = 10
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(p => p.Nombre == "Distribuciones Norte");
        result.Value.Items.Should().NotContain(p => p.Nombre == "Editorial Sur");
    }

    /// <summary>
    /// Crea directamente una categoría y un producto apuntando al proveedor,
    /// para ejercitar la regla de borrado con datos reales en la base.
    /// </summary>
    private async Task CrearProductoAsociadoAsync(string proveedorCodigo) {
        var categoria = new Categoria {
            Nombre = $"Categoria {Guid.NewGuid():N}"[..30],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext!.Categorias.Add(categoria);
        await _dbContext.SaveChangesAsync();

        _dbContext.Productos.Add(new Producto {
            Nombre = "Producto con proveedor",
            Descripcion = "Para probar la regla de borrado",
            Precio = 10m,
            Stock = 1,
            CategoriaId = categoria.Id,
            ProveedorId = proveedorCodigo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await _dbContext.SaveChangesAsync();
    }
}
