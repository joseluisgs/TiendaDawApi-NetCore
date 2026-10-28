using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Data.Seed.Sql;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Data;

/// <summary>
/// Tests del seed condicional de SqlSeeder según Auth:Mode.
///
/// 🎓 Los hashes no son intercambiables: Custom usa BCrypt ($2a$...) e Identity
/// usa PBKDF2 (AQAAAA...). Si el seed no elige el algoritmo correcto, los
/// usuarios no pueden hacer login.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Data")]
[Category("Auth")]
public class SqlSeederTests
{
    private static IConfiguration BuildConfiguration(string? authMode)
    {
        var data = new Dictionary<string, string?>();
        if (authMode is not null)
            data["Auth:Mode"] = authMode;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static TiendaDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TiendaDbContext(options);
    }

    #region Modo Custom (default)

    [Test]
    public async Task SeedAsync_ModoCustom_DebeCrearUsuariosConHashBcrypt()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        var seeder = new SqlSeeder(
            context,
            Mock.Of<ILogger<SqlSeeder>>(),
            BuildConfiguration("Custom"),
            Mock.Of<IServiceScopeFactory>());

        await seeder.SeedAsync();

        var users = context.Users.ToList();

        users.Should().HaveCount(2, "el seed crea admin y userdaw");
        users.Should().OnlyContain(u => u.PasswordHash!.StartsWith("$2a$"),
            "el modo Custom debe generar hashes BCrypt");
    }

    [Test]
    public async Task SeedAsync_ModoCustom_LasPasswordsDebenSerVerificables()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        var seeder = new SqlSeeder(
            context,
            Mock.Of<ILogger<SqlSeeder>>(),
            BuildConfiguration("Custom"),
            Mock.Of<IServiceScopeFactory>());

        await seeder.SeedAsync();

        var admin = context.Users.First(u => u.UserName == "admin");

        // Si BCrypt no verifica, nadie puede hacer login en modo Custom
        BCrypt.Net.BCrypt.Verify("admin", admin.PasswordHash!)
            .Should().BeTrue("la contraseña seed del admin en modo Custom es 'admin'");
    }

    [Test]
    public async Task SeedAsync_SinAuthMode_DebeComportarseComoCustom()
    {
        // Sin "Auth:Mode" en configuración → default "Custom"
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        var seeder = new SqlSeeder(
            context,
            Mock.Of<ILogger<SqlSeeder>>(),
            BuildConfiguration(authMode: null),
            Mock.Of<IServiceScopeFactory>());

        await seeder.SeedAsync();

        var users = context.Users.ToList();

        users.Should().HaveCount(2);
        users.Should().OnlyContain(u => u.PasswordHash!.StartsWith("$2a$"),
            "el modo por defecto debe usar BCrypt");
    }

    #endregion

    #region Modo Identity

    [Test]
    public async Task SeedAsync_ModoIdentity_DebeCrearUsuariosConHashPbkdf2()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = BuildIdentityContainer(dbName, "Identity");

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        // Nuevo scope para leer: el contexto del seeder pudo cachear entidades
        using var assertScope = provider.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        var users = context.Users.ToList();

        users.Should().HaveCount(2, "el seed crea admin y userdaw en modo Identity");
        users.Should().OnlyContain(u => u.PasswordHash!.StartsWith("AQAAAA"),
            "Identity genera hashes PBKDF2, jamés BCrypt");
    }

    [Test]
    public async Task SeedAsync_ModoIdentity_LasPasswordsDebenSerVerificables()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = BuildIdentityContainer(dbName, "Identity");

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        using var assertScope = provider.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        var userManager = assertScope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var admin = context.Users.First(u => u.UserName == "admin");

        // Política de Identity: ≥6 chars, dígito y mayúscula → "admin" NO cumple,
        // por eso el seed Identity usa "Admin123!" (ver SqlSeeder).
        var passwordOk = await userManager.CheckPasswordAsync(admin, "Admin123!");
        var passwordAdminVieja = await userManager.CheckPasswordAsync(admin, "admin");

        passwordOk.Should().BeTrue("la contraseña seed en modo Identity debe validar con UserManager");
        passwordAdminVieja.Should().BeFalse("la contraseña del modo Custom NO debe funcionar en Identity");
    }

    [Test]
    public async Task SeedAsync_ModoIdentity_DebeCrearRolesAntesDeAsignarlos()
    {
        // AddToRoleAsync lanza excepción si el rol no existe → el seed debe crearlos
        var dbName = Guid.NewGuid().ToString();
        var services = BuildIdentityContainer(dbName, "Identity");

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        using var assertScope = provider.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        var roleManager = assertScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

        (await roleManager.RoleExistsAsync(UserRoles.ADMIN)).Should().BeTrue();
        (await roleManager.RoleExistsAsync(UserRoles.USER)).Should().BeTrue();

        var admin = context.Users.First(u => u.UserName == "admin");
        admin.Role.Should().Be(UserRoles.ADMIN);
    }

    [Test]
    public async Task SeedAsync_ModoIdentity_NoDebeSembrarUsuariosConHashBcrypt()
    {
        // Regresión: si alguien "arreglara" el seed Identity reutilizando los hashes
        // hardcoded de Custom, nadie podría hacer login. Este test lo caza.
        var dbName = Guid.NewGuid().ToString();
        var services = BuildIdentityContainer(dbName, "Identity");

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        using var assertScope = provider.CreateScope();
        var context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();

        context.Users.Should().NotContain(u => u.PasswordHash!.StartsWith("$2a$"),
            "los hashes BCrypt son incompatibles con UserManager");
    }

    #endregion

    #region Seed común (categorías y productos)

    [Test]
    public async Task SeedAsync_CualquierModo_DebeSembrarCategoriasYProductos()
    {
        // El catch de SeedAsync traga excepciones: si la rama de usuarios falla
        // (p.ej. roles inexistentes en Identity), categorías y productos NO se
        // sembrarían. Este test detecta esa regresión.
        foreach (var mode in new[] { "Custom", "Identity" })
        {
            var dbName = Guid.NewGuid().ToString();

            if (mode == "Custom")
            {
                using var context = CreateContext(dbName);
                var seeder = new SqlSeeder(
                    context,
                    Mock.Of<ILogger<SqlSeeder>>(),
                    BuildConfiguration(mode),
                    Mock.Of<IServiceScopeFactory>());

                await seeder.SeedAsync();

                context.Categorias.Should().HaveCount(3, $"modo {mode} debe sembrar categorías");
                context.Productos.Should().HaveCount(3, $"modo {mode} debe sembrar productos");
            }
            else
            {
                var services = BuildIdentityContainer(dbName, mode);
                await using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();

                var seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
                await seeder.SeedAsync();

                using var assertScope = provider.CreateScope();
                var context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();

                context.Categorias.Should().HaveCount(3, $"modo {mode} debe sembrar categorías");
                context.Productos.Should().HaveCount(3, $"modo {mode} debe sembrar productos");
            }
        }
    }

    #endregion

    /// <summary>
    /// Contenedor DI mínimo con Identity + EF InMemory para ejecutar la rama
    /// Identity del seeder (necesita UserManager y RoleManager reales).
    /// </summary>
    private static ServiceCollection BuildIdentityContainer(string dbName, string authMode)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<TiendaDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        services.AddIdentity<User, IdentityRole<long>>()
            .AddEntityFrameworkStores<TiendaDbContext>()
            .AddDefaultTokenProviders();

        var configuration = BuildConfiguration(authMode);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<SqlSeeder>();

        return services;
    }
}
