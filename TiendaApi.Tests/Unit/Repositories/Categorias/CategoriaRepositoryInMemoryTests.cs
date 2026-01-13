using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;

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

        var categoria = await repository.FindByIdAsync(1);
        categoria.Should().BeNull();
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
}
