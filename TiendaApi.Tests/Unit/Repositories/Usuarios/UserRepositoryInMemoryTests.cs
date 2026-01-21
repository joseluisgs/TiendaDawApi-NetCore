using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;

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

    #region FindAllPagedAsync Tests

    [Test]
    public async Task FindAllPagedAsync_Con20Usuarios_Pagina1_Size10_Retorna10()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        for (int i = 1; i <= 20; i++)
        {
            context.Users.Add(new User { Id = i, Username = $"user{i}", Email = $"user{i}@test.com" });
        }
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(10);
        totalCount.Should().Be(20);
    }

    [Test]
    public async Task FindAllPagedAsync_Pagina2_Size5_RetornaSiguientes5()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        for (int i = 1; i <= 20; i++)
        {
            context.Users.Add(new User { Id = i, Username = $"user{i}", Email = $"user{i}@test.com" });
        }
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto(null, null, null, 1, 5, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(5);
        items.First().Id.Should().Be(6);
        items.Last().Id.Should().Be(10);
    }

    [Test]
    public async Task FindAllPagedAsync_ConFiltroUsername_RetornaSoloCoincidentes()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Users.AddRange(
            new User { Id = 1, Username = "admin", Email = "admin@test.com" },
            new User { Id = 2, Username = "adminuser", Email = "adminuser@test.com" },
            new User { Id = 3, Username = "guest", Email = "guest@test.com" },
            new User { Id = 4, Username = "otheruser", Email = "other@test.com" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto("admin", null, null, 0, 10, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_ConFiltroEmail_RetornaSoloCoincidentes()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Users.AddRange(
            new User { Id = 1, Username = "user1", Email = "admin@empresa.com" },
            new User { Id = 2, Username = "user2", Email = "user@empresa.com" },
            new User { Id = 3, Username = "user3", Email = "test@gmail.com" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto(null, "empresa.com", null, 0, 10, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_ConFiltroIsDeleted_RetornaSoloEliminados()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Users.AddRange(
            new User { Id = 1, Username = "activo", Email = "activo@test.com", IsDeleted = false },
            new User { Id = 2, Username = "eliminado1", Email = "del1@test.com", IsDeleted = true },
            new User { Id = 3, Username = "eliminado2", Email = "del2@test.com", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto(null, null, true, 0, 10, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(2);
        items.All(u => u.IsDeleted).Should().BeTrue();
    }

    [Test]
    public async Task FindAllPagedAsync_OrdenacionDescendente_RetornaOrdenInverso()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Users.AddRange(
            new User { Id = 1, Username = "aaa" },
            new User { Id = 2, Username = "bbb" },
            new User { Id = 3, Username = "ccc" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "desc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.First().Id.Should().Be(3);
        items.Last().Id.Should().Be(1);
    }

    [Test]
    public async Task FindAllPagedAsync_OrdenacionPorUsername_RetornaOrdenadoPorUsername()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Users.AddRange(
            new User { Id = 1, Username = "zebra" },
            new User { Id = 2, Username = "alfa" },
            new User { Id = 3, Username = "beta" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto(null, null, null, 0, 10, "username", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.First().Username.Should().Be("alfa");
        items.Skip(1).First().Username.Should().Be("beta");
        items.Last().Username.Should().Be("zebra");
    }

    [Test]
    public async Task FindAllPagedAsync_PaginaVacia_RetornaCeroElementos()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Users.AddRange(
            new User { Id = 1, Username = "user1" },
            new User { Id = 2, Username = "user2" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto(null, null, null, 10, 10, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().BeEmpty();
        totalCount.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_FiltrosCombinados_TotalCountEsCorrecto()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Users.AddRange(
            new User { Id = 1, Username = "admin", Email = "admin@test.com", IsDeleted = false },
            new User { Id = 2, Username = "admin2", Email = "admin2@test.com", IsDeleted = false },
            new User { Id = 3, Username = "user", Email = "user@test.com", IsDeleted = false },
            new User { Id = 4, Username = "adminold", Email = "old@test.com", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());
        var filter = new UserFilterDto("admin", "test.com", false, 0, 10, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(2);
        items.All(u => u.Username.Contains("admin") && u.Email.Contains("test.com") && !u.IsDeleted).Should().BeTrue();
        totalCount.Should().Be(2);
    }

    #endregion

    #region GetActiveUsersAsync Tests

    /// <summary>
    /// Verifica que retorna solo usuarios activos.
    /// </summary>
    [Test]
    public async Task GetActiveUsersAsync_ConUsuariosActivos_RetornaSoloActivos()
    {
        using var context = CreateContext(nameof(GetActiveUsersAsync_ConUsuariosActivos_RetornaSoloActivos));

        context.Users.AddRange(
            new User { Id = 1, Username = "admin", Email = "admin@test.com" },
            new User { Id = 2, Username = "user1", Email = "user1@test.com" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.GetActiveUsersAsync()).ToList();

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifica que excluye usuarios eliminados lógicamente.
    /// </summary>
    [Test]
    public async Task GetActiveUsersAsync_ConUsuariosEliminados_ExcluyeEliminados()
    {
        using var context = CreateContext(nameof(GetActiveUsersAsync_ConUsuariosEliminados_ExcluyeEliminados));

        context.Users.AddRange(
            new User { Id = 1, Username = "activo", Email = "activo@test.com", IsDeleted = false },
            new User { Id = 2, Username = "eliminado", Email = "eliminado@test.com", IsDeleted = true },
            new User { Id = 3, Username = "otro", Email = "otro@test.com", IsDeleted = false }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.GetActiveUsersAsync()).ToList();

        result.Should().HaveCount(2);
        result.All(u => !u.IsDeleted).Should().BeTrue();
    }

    /// <summary>
    /// Verifica que retorna lista vacía cuando no hay usuarios activos.
    /// </summary>
    [Test]
    public async Task GetActiveUsersAsync_SoloEliminados_RetornaListaVacia()
    {
        using var context = CreateContext(nameof(GetActiveUsersAsync_SoloEliminados_RetornaListaVacia));

        context.Users.AddRange(
            new User { Id = 1, Username = "eliminado1", Email = "del1@test.com", IsDeleted = true },
            new User { Id = 2, Username = "eliminado2", Email = "del2@test.com", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.GetActiveUsersAsync()).ToList();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que retorna lista vacía cuando no hay usuarios.
    /// </summary>
    [Test]
    public async Task GetActiveUsersAsync_SinUsuarios_RetornaListaVacia()
    {
        using var context = CreateContext(nameof(GetActiveUsersAsync_SinUsuarios_RetornaListaVacia));

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.GetActiveUsersAsync()).ToList();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que usuarios están ordenados por Email.
    /// </summary>
    [Test]
    public async Task GetActiveUsersAsync_MultiplesUsuarios_OrdenadosPorEmail()
    {
        using var context = CreateContext(nameof(GetActiveUsersAsync_MultiplesUsuarios_OrdenadosPorEmail));

        context.Users.AddRange(
            new User { Id = 1, Username = "zebra", Email = "zebra@test.com" },
            new User { Id = 2, Username = "alfa", Email = "alfa@test.com" },
            new User { Id = 3, Username = "beta", Email = "beta@test.com" }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.GetActiveUsersAsync()).ToList();

        result.Should().HaveCount(3);
        result[0].Email.Should().Be("alfa@test.com");
        result[1].Email.Should().Be("beta@test.com");
        result[2].Email.Should().Be("zebra@test.com");
    }

    /// <summary>
    /// Verifica que con usuarios mixtos solo retorna activos.
    /// </summary>
    [Test]
    public async Task GetActiveUsersAsync_UsuariosMixtos_RetornaSoloActivos()
    {
        using var context = CreateContext(nameof(GetActiveUsersAsync_UsuariosMixtos_RetornaSoloActivos));

        context.Users.AddRange(
            new User { Id = 1, Username = "a1", Email = "a1@test.com", IsDeleted = false },
            new User { Id = 2, Username = "d1", Email = "d1@test.com", IsDeleted = true },
            new User { Id = 3, Username = "a2", Email = "a2@test.com", IsDeleted = false },
            new User { Id = 4, Username = "d2", Email = "d2@test.com", IsDeleted = true },
            new User { Id = 5, Username = "a3", Email = "a3@test.com", IsDeleted = false }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.GetActiveUsersAsync()).ToList();

        result.Should().HaveCount(3);
        result.All(u => !u.IsDeleted).Should().BeTrue();
    }

    /// <summary>
    /// Verifica que retorna un solo usuario activo.
    /// </summary>
    [Test]
    public async Task GetActiveUsersAsync_UnUsuarioActivo_RetornaEseUsuario()
    {
        using var context = CreateContext(nameof(GetActiveUsersAsync_UnUsuarioActivo_RetornaEseUsuario));

        context.Users.AddRange(
            new User { Id = 1, Username = "solo", Email = "solo@test.com", IsDeleted = false },
            new User { Id = 2, Username = "eliminado", Email = "del@test.com", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new UserRepository(context, Mock.Of<ILogger<UserRepository>>());

        var result = (await repository.GetActiveUsersAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("solo");
    }

    #endregion
}
