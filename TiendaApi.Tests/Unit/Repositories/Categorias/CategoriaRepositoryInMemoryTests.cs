using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Tests.Unit.Repositories.Categorias;

public class CategoriaRepositoryInMemoryTests
{
    private TiendaDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TiendaDbContext(options);
    }

    [Test]
    public async Task FindAllAsync_SinCategorias_RetornaListaVacia()
    {
        using var context = CreateContext(nameof(FindAllAsync_SinCategorias_RetornaListaVacia));

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());

        var result = await repository.FindAllAsync();

        result.Should().BeEmpty();
    }

    [Test]
    public async Task FindAllAsync_ConCategorias_RetornaListaOrdenada()
    {
        using var context = CreateContext(nameof(FindAllAsync_ConCategorias_RetornaListaOrdenada));

        context.Categorias.AddRange(
            new Categoria { Id = 2, Nombre = "Electrónica" },
            new Categoria { Id = 1, Nombre = "Ropa" }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());

        var result = (await repository.FindAllAsync()).ToList();

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task FindByIdAsync_Existe_RetornaCategoria()
    {
        using var context = CreateContext(nameof(FindByIdAsync_Existe_RetornaCategoria));

        context.Categorias.Add(new Categoria { Id = 1, Nombre = "Electrónica" });
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());

        var result = await repository.FindByIdAsync(1);

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Electrónica");
    }

    [Test]
    public async Task FindByIdAsync_NoExiste_RetornaNull()
    {
        using var context = CreateContext(nameof(FindByIdAsync_NoExiste_RetornaNull));

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());

        var result = await repository.FindByIdAsync(999);

        result.Should().BeNull();
    }

    [Test]
    public async Task SaveAsync_NuevaCategoria_RetornaConId()
    {
        using var context = CreateContext(nameof(SaveAsync_NuevaCategoria_RetornaConId));

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var categoria = new Categoria { Nombre = "Nueva Categoría" };

        var result = await repository.SaveAsync(categoria);

        result.Id.Should().BeGreaterThan(0);
        result.Nombre.Should().Be("Nueva Categoría");
    }

    [Test]
    public async Task UpdateAsync_Existente_ActualizaNombre()
    {
        using var context = CreateContext(nameof(UpdateAsync_Existente_ActualizaNombre));

        context.Categorias.Add(new Categoria { Id = 1, Nombre = "Original" });
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var categoria = await repository.FindByIdAsync(1);
        categoria!.Nombre = "Actualizado";

        var result = await repository.UpdateAsync(categoria);

        result.Nombre.Should().Be("Actualizado");
    }

    [Test]
    public async Task DeleteAsync_Existente_MarcaIsDeleted()
    {
        using var context = CreateContext(nameof(DeleteAsync_Existente_MarcaIsDeleted));

        context.Categorias.Add(new Categoria { Id = 1, Nombre = "Para Borrar" });
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());

        await repository.DeleteAsync(1);

        var categoria = await context.Categorias.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == 1);
        categoria.Should().NotBeNull();
        categoria!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task FindAllAsync_NoMuestraEliminados_SoftDelete()
    {
        using var context = CreateContext(nameof(FindAllAsync_NoMuestraEliminados_SoftDelete));

        context.Categorias.AddRange(
            new Categoria { Id = 1, Nombre = "Activa" },
            new Categoria { Id = 2, Nombre = "Eliminada", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());

        var result = (await repository.FindAllAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Activa");
    }

    #region FindAllPagedAsync Tests

    [Test]
    public async Task FindAllPagedAsync_Con15Categorias_Pagina1_Size5_Retorna5()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        for (int i = 1; i <= 15; i++)
        {
            context.Categorias.Add(new Categoria { Id = i, Nombre = $"Categoría {i}" });
        }
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto { Page = 0, Size = 5, SortBy = "id", Direction = "asc" };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(5);
        totalCount.Should().Be(15);
    }

    [Test]
    public async Task FindAllPagedAsync_Pagina2_Size5_RetornaSiguientes5()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        for (int i = 1; i <= 15; i++)
        {
            context.Categorias.Add(new Categoria { Id = i, Nombre = $"Categoría {i}" });
        }
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto { Page = 1, Size = 5, SortBy = "id", Direction = "asc" };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(5);
        items.First().Id.Should().Be(6);
        items.Last().Id.Should().Be(10);
    }

    [Test]
    public async Task FindAllPagedAsync_ConFiltroNombre_RetornaSoloCoincidentes()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Categorias.AddRange(
            new Categoria { Id = 1, Nombre = "Electrónica" },
            new Categoria { Id = 2, Nombre = "Electrónica de Consumo" },
            new Categoria { Id = 3, Nombre = "Ropa" },
            new Categoria { Id = 4, Nombre = "Calzado Deportivo" }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto { Nombre = "Electrónica", Page = 0, Size = 10, SortBy = "id", Direction = "asc" };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_ConFiltroIsDeleted_RetornaSoloEliminados()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Categorias.AddRange(
            new Categoria { Id = 1, Nombre = "Activa", IsDeleted = false },
            new Categoria { Id = 2, Nombre = "Eliminada 1", IsDeleted = true },
            new Categoria { Id = 3, Nombre = "Eliminada 2", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto { IsDeleted = true, Page = 0, Size = 10, SortBy = "id", Direction = "asc" };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(2);
        items.All(c => c.IsDeleted).Should().BeTrue();
    }

    [Test]
    public async Task FindAllPagedAsync_OrdenacionDescendente_RetornaOrdenInverso()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Categorias.AddRange(
            new Categoria { Id = 1, Nombre = "Primera" },
            new Categoria { Id = 2, Nombre = "Segunda" },
            new Categoria { Id = 3, Nombre = "Tercera" }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto { Page = 0, Size = 10, SortBy = "id", Direction = "desc" };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.First().Id.Should().Be(3);
        items.Last().Id.Should().Be(1);
    }

    [Test]
    public async Task FindAllPagedAsync_OrdenacionPorNombre_RetornaOrdenadoPorNombre()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Categorias.AddRange(
            new Categoria { Id = 1, Nombre = "Zebra" },
            new Categoria { Id = 2, Nombre = "Alfa" },
            new Categoria { Id = 3, Nombre = "Beta" }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto { Page = 0, Size = 10, SortBy = "nombre", Direction = "asc" };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.First().Nombre.Should().Be("Alfa");
        items.Skip(1).First().Nombre.Should().Be("Beta");
        items.Last().Nombre.Should().Be("Zebra");
    }

    [Test]
    public async Task FindAllPagedAsync_PaginaVacia_RetornaCeroElementos()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Categorias.AddRange(
            new Categoria { Id = 1, Nombre = "Categoría 1" },
            new Categoria { Id = 2, Nombre = "Categoría 2" }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto { Page = 10, Size = 10, SortBy = "id", Direction = "asc" };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().BeEmpty();
        totalCount.Should().Be(2);
    }

    [Test]
    public async Task FindAllPagedAsync_FiltrosCombinados_TotalCountEsCorrecto()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Categorias.AddRange(
            new Categoria { Id = 1, Nombre = "Electrónica", IsDeleted = false },
            new Categoria { Id = 2, Nombre = "Electrónica Vintage", IsDeleted = false },
            new Categoria { Id = 3, Nombre = "Ropa", IsDeleted = false },
            new Categoria { Id = 4, Nombre = "Electrónica Antigua", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var repository = new CategoriaRepository(context, Mock.Of<ILogger<CategoriaRepository>>());
        var filter = new CategoriaFilterDto
        {
            Nombre = "Electrónica",
            IsDeleted = false,
            Page = 0,
            Size = 10,
            SortBy = "id",
            Direction = "asc"
        };

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().HaveCount(2);
        items.All(c => c.Nombre.Contains("Electrónica") && !c.IsDeleted).Should().BeTrue();
        totalCount.Should().Be(2);
    }

    #endregion
}
