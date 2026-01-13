using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Usuarios;

namespace TiendaApi.Tests.Unit.Repositories.Usuarios;

public class UserRepositoryInMemoryTests
{
    private TiendaDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TiendaDbContext(options);
    }

    [Test]
    public async Task FindByIdAsync_Existe_RetornaUsuario()
    {
        using var context = CreateContext(nameof(FindByIdAsync_Existe_RetornaUsuario));

        context.Users.Add(new User { Id = 1, Username = "admin", Email = "admin@test.com" });
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = await repository.FindByIdAsync(1);

        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
    }

    [Test]
    public async Task FindByIdAsync_NoExiste_RetornaNull()
    {
        using var context = CreateContext(nameof(FindByIdAsync_NoExiste_RetornaNull));

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = await repository.FindByIdAsync(999);

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByUsernameAsync_Existe_RetornaUsuario()
    {
        using var context = CreateContext(nameof(FindByUsernameAsync_Existe_RetornaUsuario));

        context.Users.Add(new User { Id = 1, Username = "admin", Email = "admin@test.com" });
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = await repository.FindByUsernameAsync("admin");

        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
    }

    [Test]
    public async Task FindByUsernameAsync_NoExiste_RetornaNull()
    {
        using var context = CreateContext(nameof(FindByUsernameAsync_NoExiste_RetornaNull));

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = await repository.FindByUsernameAsync("noexiste");

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByEmailAsync_Existe_RetornaUsuario()
    {
        using var context = CreateContext(nameof(FindByEmailAsync_Existe_RetornaUsuario));

        context.Users.Add(new User { Id = 1, Username = "admin", Email = "admin@test.com" });
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = await repository.FindByEmailAsync("admin@test.com");

        result.Should().NotBeNull();
        result!.Email.Should().Be("admin@test.com");
    }

    [Test]
    public async Task SaveAsync_NuevoUsuario_RetornaConId()
    {
        using var context = CreateContext(nameof(SaveAsync_NuevoUsuario_RetornaConId));

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var user = new User { Username = "newuser", Email = "new@test.com", PasswordHash = "hash" };

        var result = await repository.SaveAsync(user);

        result.Id.Should().BeGreaterThan(0);
        result.Username.Should().Be("newuser");
    }

    [Test]
    public async Task UpdateAsync_Existente_ActualizaDatos()
    {
        using var context = CreateContext(nameof(UpdateAsync_Existente_ActualizaDatos));

        context.Users.Add(new User { Id = 1, Username = "original", Email = "original@test.com" });
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var user = await repository.FindByIdAsync(1);
        user!.Username = "actualizado";

        var result = await repository.UpdateAsync(user);

        result.Username.Should().Be("actualizado");
    }

    [Test]
    public async Task FindAllAsync_ConUsuarios_RetornaLista()
    {
        using var context = CreateContext(nameof(FindAllAsync_ConUsuarios_RetornaLista));

        context.Users.AddRange(
            new User { Id = 1, Username = "admin" },
            new User { Id = 2, Username = "user" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.FindAllAsync()).ToList();

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAllAsync_NoMuestraEliminados_SoftDelete()
    {
        using var context = CreateContext(nameof(FindAllAsync_NoMuestraEliminados_SoftDelete));

        context.Users.AddRange(
            new User { Id = 1, Username = "activo" },
            new User { Id = 2, Username = "eliminado", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.FindAllAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("activo");
    }
}
