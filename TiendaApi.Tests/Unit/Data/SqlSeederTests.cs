using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Data.Seed;
using TiendaApi.Api.Data.Seed.Sql;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Data;

/// <summary>
///     Tests del seed condicional de SqlSeeder según Auth:Mode.
///     🎓 Los hashes no son intercambiables: Custom usa BCrypt ($2a$...) e Identity
///     usa PBKDF2 (AQAAAA...). Si el seed no elige el algoritmo correcto, los
///     usuarios no pueden hacer login.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Data")]
[Category("Auth")]
public class SqlSeederTests {
    private static IConfiguration BuildConfiguration(string? authMode) {
        var data = new Dictionary<string, string?>();
        if (authMode is not null) {
            data["Auth:Mode"] = authMode;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static TiendaDbContext CreateContext(string dbName) {
        DbContextOptions<TiendaDbContext> options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TiendaDbContext(options);
    }

    [Test]
    public async Task SeedAsync_ModoCustom_DebeCrearUsuariosConHashBcrypt() {
        string dbName = Guid.NewGuid().ToString();
        using TiendaDbContext context = CreateContext(dbName);

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
    public async Task SeedAsync_ModoCustom_LasPasswordsDebenSerVerificables() {
        string dbName = Guid.NewGuid().ToString();
        using TiendaDbContext context = CreateContext(dbName);

        var seeder = new SqlSeeder(
            context,
            Mock.Of<ILogger<SqlSeeder>>(),
            BuildConfiguration("Custom"),
            Mock.Of<IServiceScopeFactory>());

        await seeder.SeedAsync();

        User admin = context.Users.First(u => u.UserName == SeedCredentials.AdminUsername);
        User user = context.Users.First(u => u.UserName == SeedCredentials.UserUsername);

        // Si BCrypt no verifica, nadie puede hacer login en modo Custom.
        // Credenciales unificadas: las MISMAS que en Identity (SeedCredentials).
        BCrypt.Net.BCrypt.Verify(SeedCredentials.AdminPassword, admin.PasswordHash!)
            .Should().BeTrue("la contraseña seed del admin debe verificar con su hash BCrypt");
        BCrypt.Net.BCrypt.Verify(SeedCredentials.UserPassword, user.PasswordHash!)
            .Should().BeTrue("la contraseña seed del userdaw debe verificar con su hash BCrypt");
    }

    [Test]
    public async Task SeedAsync_SinAuthMode_DebeComportarseComoCustom() {
        // Sin "Auth:Mode" en configuración → default "Custom"
        string dbName = Guid.NewGuid().ToString();
        using TiendaDbContext context = CreateContext(dbName);

        var seeder = new SqlSeeder(
            context,
            Mock.Of<ILogger<SqlSeeder>>(),
            BuildConfiguration(null),
            Mock.Of<IServiceScopeFactory>());

        await seeder.SeedAsync();

        var users = context.Users.ToList();

        users.Should().HaveCount(2);
        users.Should().OnlyContain(u => u.PasswordHash!.StartsWith("$2a$"),
            "el modo por defecto debe usar BCrypt");
    }

    [Test]
    public async Task SeedAsync_ModoIdentity_DebeCrearUsuariosConHashPbkdf2() {
        string dbName = Guid.NewGuid().ToString();
        ServiceCollection services = BuildIdentityContainer(dbName, "Identity");

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        SqlSeeder seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        // Nuevo scope para leer: el contexto del seeder pudo cachear entidades
        using IServiceScope assertScope = provider.CreateScope();
        TiendaDbContext context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        var users = context.Users.ToList();

        users.Should().HaveCount(2, "el seed crea admin y userdaw en modo Identity");
        users.Should().OnlyContain(u => u.PasswordHash!.StartsWith("AQAAAA"),
            "Identity genera hashes PBKDF2, jamés BCrypt");
    }

    [Test]
    public async Task SeedAsync_ModoIdentity_LasPasswordsDebenSerVerificables() {
        string dbName = Guid.NewGuid().ToString();
        ServiceCollection services = BuildIdentityContainer(dbName, "Identity");

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        SqlSeeder seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        using IServiceScope assertScope = provider.CreateScope();
        TiendaDbContext context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        UserManager<User> userManager = assertScope.ServiceProvider.GetRequiredService<UserManager<User>>();

        User admin = context.Users.First(u => u.UserName == SeedCredentials.AdminUsername);
        User user = context.Users.First(u => u.UserName == SeedCredentials.UserUsername);

        // Credenciales unificadas: SeedCredentials cumple la política de Identity
        // (≥8, mayúscula y dígito) y es la MISMA en los dos modos. Las contraseñas
        // cortas antiguas ("admin") NO deben validar.
        bool adminOk = await userManager.CheckPasswordAsync(admin, SeedCredentials.AdminPassword);
        bool userOk = await userManager.CheckPasswordAsync(user, SeedCredentials.UserPassword);
        bool passwordCortaVieja = await userManager.CheckPasswordAsync(admin, "admin");

        adminOk.Should().BeTrue("la contraseña del admin (SeedCredentials) debe validar con UserManager");
        userOk.Should().BeTrue("la contraseña del userdaw (SeedCredentials) debe validar con UserManager");
        passwordCortaVieja.Should().BeFalse("las contraseñas cortas antiguas NO deben funcionar");
    }

    [Test]
    public async Task SeedAsync_ModoIdentity_DebeCrearRolesAntesDeAsignarlos() {
        // AddToRoleAsync lanza excepción si el rol no existe → el seed debe crearlos
        string dbName = Guid.NewGuid().ToString();
        ServiceCollection services = BuildIdentityContainer(dbName, "Identity");

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        SqlSeeder seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        using IServiceScope assertScope = provider.CreateScope();
        TiendaDbContext context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        RoleManager<IdentityRole<long>> roleManager =
            assertScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

        (await roleManager.RoleExistsAsync(UserRoles.ADMIN)).Should().BeTrue();
        (await roleManager.RoleExistsAsync(UserRoles.USER)).Should().BeTrue();

        User admin = context.Users.First(u => u.UserName == "admin");
        admin.Role.Should().Be(UserRoles.ADMIN);
    }

    [Test]
    public async Task SeedAsync_ModoIdentity_NoDebeSembrarUsuariosConHashBcrypt() {
        // Regresión: si alguien "arreglara" el seed Identity reutilizando los hashes
        // hardcoded de Custom, nadie podría hacer login. Este test lo caza.
        string dbName = Guid.NewGuid().ToString();
        ServiceCollection services = BuildIdentityContainer(dbName, "Identity");

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        SqlSeeder seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        await seeder.SeedAsync();

        using IServiceScope assertScope = provider.CreateScope();
        TiendaDbContext context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();

        context.Users.Should().NotContain(u => u.PasswordHash!.StartsWith("$2a$"),
            "los hashes BCrypt son incompatibles con UserManager");
    }

    [Test]
    public async Task SeedAsync_CualquierModo_DebeSembrarCategoriasYProductos() {
        // El catch de SeedAsync traga excepciones: si la rama de usuarios falla
        // (p.ej. roles inexistentes en Identity), categorías y productos NO se
        // sembrarían. Este test detecta esa regresión.
        foreach (string mode in new[] { "Custom", "Identity" }) {
            string dbName = Guid.NewGuid().ToString();

            if (mode == "Custom") {
                using TiendaDbContext context = CreateContext(dbName);
                var seeder = new SqlSeeder(
                    context,
                    Mock.Of<ILogger<SqlSeeder>>(),
                    BuildConfiguration(mode),
                    Mock.Of<IServiceScopeFactory>());

                await seeder.SeedAsync();

                context.Categorias.Should().HaveCount(3, $"modo {mode} debe sembrar categorías");
                context.Productos.Should().HaveCount(3, $"modo {mode} debe sembrar productos");
            } else {
                ServiceCollection services = BuildIdentityContainer(dbName, mode);
                await using ServiceProvider provider = services.BuildServiceProvider();
                using IServiceScope scope = provider.CreateScope();

                SqlSeeder seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
                await seeder.SeedAsync();

                using IServiceScope assertScope = provider.CreateScope();
                TiendaDbContext context = assertScope.ServiceProvider.GetRequiredService<TiendaDbContext>();

                context.Categorias.Should().HaveCount(3, $"modo {mode} debe sembrar categorías");
                context.Productos.Should().HaveCount(3, $"modo {mode} debe sembrar productos");
            }
        }
    }

    /// <summary>
    ///     Contenedor DI mínimo con Identity + EF InMemory para ejecutar la rama
    ///     Identity del seeder (necesita UserManager y RoleManager reales).
    /// </summary>
    private static ServiceCollection BuildIdentityContainer(string dbName, string authMode) {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<TiendaDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddIdentity<User, IdentityRole<long>>()
            .AddEntityFrameworkStores<TiendaDbContext>()
            .AddDefaultTokenProviders();

        IConfiguration configuration = BuildConfiguration(authMode);
        services.AddSingleton(configuration);
        services.AddScoped<SqlSeeder>();

        return services;
    }
}
