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
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Users;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Integration.TestContainers.Usuarios.Services;

/// <summary>
/// Tests de integración para UserService con DI completo.
/// Verifica el servicio con base de datos real usando Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class UserServiceIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IUserService? _userService;

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
                { "Cache:UsuarioCacheTTLMinutes", "10" }
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

        services.AddScoped<ILogger<UserRepository>, Logger<UserRepository>>();
        services.AddScoped<ILogger<UserService>, Logger<UserService>>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();
        services.AddScoped<ICacheService, MemoryCacheService>();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        _userService = _serviceProvider.GetRequiredService<IUserService>();
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
    public async Task FindAllAsync_SinUsuarios_RetornaListaVacia()
    {
        var result = await _userService!.FindAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task CreateAsync_ConDatosValidos_RetornaUsuarioCreado()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var dto = new RegisterDto
        {
            Username = $"testuser_{uniqueId}",
            Email = $"test_{uniqueId}@example.com",
            Password = "Test1234"
        };

        var result = await _userService!.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Username.Should().Be(dto.Username);
        result.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task FindByIdAsync_ConUsuarioExistente_RetornaUsuario()
    {
        var dto = new RegisterDto
        {
            Username = "buscaruser",
            Email = "buscar@example.com",
            Password = "Test1234"
        };
        var createResult = await _userService!.CreateAsync(dto);
        createResult.IsSuccess.Should().BeTrue();

        var findResult = await _userService.FindByIdAsync(createResult.Value.Id);

        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Username.Should().Be("buscaruser");
    }

    [Test]
    public async Task FindByIdAsync_ConUsuarioNoExistente_RetornaNotFound()
    {
        var result = await _userService!.FindByIdAsync(999999);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConEmailDuplicado_RetornaConflicto()
    {
        var dto = new RegisterDto
        {
            Username = "user1",
            Email = "duplicate@example.com",
            Password = "Test1234"
        };
        await _userService!.CreateAsync(dto);

        var result = await _userService.CreateAsync(new RegisterDto
        {
            Username = "user2",
            Email = "duplicate@example.com",
            Password = "Test1234"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConUsernameDuplicado_RetornaConflicto()
    {
        var dto = new RegisterDto
        {
            Username = "duplicateuser",
            Email = "user1@example.com",
            Password = "Test1234"
        };
        await _userService!.CreateAsync(dto);

        var result = await _userService.CreateAsync(new RegisterDto
        {
            Username = "duplicateuser",
            Email = "user2@example.com",
            Password = "Test1234"
        });

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConEmailInvalido_RetornaErrorValidacion()
    {
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "email-invalido",
            Password = "Test1234"
        };

        var result = await _userService!.CreateAsync(dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConPasswordCorto_RetornaErrorValidacion()
    {
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "123"
        };

        var result = await _userService!.CreateAsync(dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConUsernameCorto_RetornaErrorValidacion()
    {
        var dto = new RegisterDto
        {
            Username = "ab",
            Email = "test@example.com",
            Password = "Test1234"
        };

        var result = await _userService!.CreateAsync(dto);

        result.IsFailure.Should().BeTrue();
    }

    #region ========== UPDATE ASYNC TESTS ==========

    [Test]
    public async Task UpdateAsync_ConUsuarioExistente_ActualizaDatos()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "updatetest",
            Email = "update@example.com",
            Password = "Test1234"
        };
        var createResult = await _userService!.CreateAsync(dto);
        createResult.IsSuccess.Should().BeTrue();
        var userId = createResult.Value.Id;

        var updateDto = new UserUpdateDto { Email = "updated@example.com" };

        // Act
        var result = await _userService.UpdateAsync(userId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("updated@example.com");
    }

    [Test]
    public async Task UpdateAsync_ConUsuarioNoExistente_RetornaNotFound()
    {
        // Arrange
        var updateDto = new UserUpdateDto { Email = "updated@example.com" };

        // Act
        var result = await _userService!.UpdateAsync(999999, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_ConEmailDuplicado_RetornaConflicto()
    {
        // Arrange
        var dto1 = new RegisterDto
        {
            Username = "user1update",
            Email = "user1update@example.com",
            Password = "Test1234"
        };
        var dto2 = new RegisterDto
        {
            Username = "user2update",
            Email = "user2update@example.com",
            Password = "Test1234"
        };
        await _userService!.CreateAsync(dto1);
        var createResult = await _userService.CreateAsync(dto2);
        var userId = createResult.Value.Id;

        var updateDto = new UserUpdateDto { Email = "user1update@example.com" };

        // Act
        var result = await _userService.UpdateAsync(userId, updateDto);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ========== DELETE ASYNC TESTS ==========

    [Test]
    public async Task DeleteAsync_ConUsuarioExistente_EliminaUsuario()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "deletetest",
            Email = "delete@example.com",
            Password = "Test1234"
        };
        var createResult = await _userService!.CreateAsync(dto);
        createResult.IsSuccess.Should().BeTrue();
        var userId = createResult.Value.Id;

        // Act
        var result = await _userService.DeleteAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var findResult = await _userService.FindByIdAsync(userId);
        findResult.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_ConUsuarioNoExistente_RetornaNotFound()
    {
        // Act
        var result = await _userService!.DeleteAsync(999999);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ========== UPDATE AVATAR ASYNC TESTS ==========

    [Test]
    public async Task UpdateAvatarAsync_ConUsuarioExistente_ActualizaAvatar()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "avatartest",
            Email = "avatar@example.com",
            Password = "Test1234"
        };
        var createResult = await _userService!.CreateAsync(dto);
        createResult.IsSuccess.Should().BeTrue();
        var userId = createResult.Value.Id;

        // Act
        var result = await _userService.UpdateAvatarAsync(userId, "/storage/uploads/avatars/new-avatar.jpg");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Avatar.Should().Be("/storage/uploads/avatars/new-avatar.jpg");
    }

    [Test]
    public async Task UpdateAvatarAsync_ConUsuarioNoExistente_RetornaNotFound()
    {
        // Act
        var result = await _userService!.UpdateAvatarAsync(999999, "/uploads/avatars/test.jpg");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ========== FIND ALL PAGED ASYNC TESTS ==========

    [Test]
    public async Task FindAllPagedAsync_SinUsuarios_RetornaPaginaVacia()
    {
        // Act
        var result = await _userService!.FindAllPagedAsync(new UserFilterDto(null, null, null, 0, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task FindAllPagedAsync_ConUsuarios_RetornaUsuariosPaginados()
    {
        // Arrange - Create multiple users
        for (int i = 0; i < 5; i++)
        {
            var dto = new RegisterDto
            {
                Username = $"pageduser{i}",
                Email = $"paged{i}@example.com",
                Password = "Test1234"
            };
            await _userService!.CreateAsync(dto);
        }

        // Act
        var result = await _userService!.FindAllPagedAsync(new UserFilterDto(null, null, null, 0, 3));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCountLessThanOrEqualTo(3);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(3);
    }

    [Test]
    public async Task FindAllPagedAsync_SegundaPagina_RetornaPaginaCorrecta()
    {
        // Arrange - Create users
        for (int i = 0; i < 5; i++)
        {
            var dto = new RegisterDto
            {
                Username = $"paged2user{i}",
                Email = $"paged2{i}@example.com",
                Password = "Test1234"
            };
            await _userService!.CreateAsync(dto);
        }

        // Act
        var result = await _userService!.FindAllPagedAsync(new UserFilterDto(null, null, null, 1, 3));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
    }

    #endregion
}
