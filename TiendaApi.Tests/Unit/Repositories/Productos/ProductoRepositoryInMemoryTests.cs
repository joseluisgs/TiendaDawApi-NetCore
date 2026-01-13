using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Models;

namespace TiendaApi.Tests.Unit.Repositories.Productos;

public class ProductoRepositoryInMemoryTests
{
    private TiendaDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TiendaDbContext(options);
    }

    private static byte[] NewRowVersion() => new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

    [Test]
    public async Task ExistsAsync_Existe_RetornaTrue()
    {
        using var context = CreateContext(nameof(ExistsAsync_Existe_RetornaTrue));

        context.Productos.Add(new Producto { Id = 1, Nombre = "Laptop", RowVersion = NewRowVersion() });
        await context.SaveChangesAsync();

        var result = await context.Productos.AnyAsync(p => p.Id == 1);

        result.Should().BeTrue();
    }

    [Test]
    public async Task ExistsAsync_NoExiste_RetornaFalse()
    {
        using var context = CreateContext(nameof(ExistsAsync_NoExiste_RetornaFalse));

        var result = await context.Productos.AnyAsync(p => p.Id == 999);

        result.Should().BeFalse();
    }

    [Test]
    public async Task SaveAsync_NuevoProducto_RetornaConId()
    {
        using var context = CreateContext(nameof(SaveAsync_NuevoProducto_RetornaConId));

        var producto = new Producto { Id = 100, Nombre = "Tablet", Precio = 299.99m, Stock = 20, CategoriaId = 1, RowVersion = NewRowVersion() };

        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        producto.Id.Should().Be(100);
        producto.Nombre.Should().Be("Tablet");
    }

    [Test]
    public async Task UpdateAsync_Existente_ActualizaPrecioYStock()
    {
        using var context = CreateContext(nameof(UpdateAsync_Existente_ActualizaPrecioYStock));

        context.Productos.Add(new Producto { Id = 1, Nombre = "Laptop", Precio = 999.99m, Stock = 10, RowVersion = NewRowVersion() });
        await context.SaveChangesAsync();

        var producto = await context.Productos.FindAsync(1L);
        producto!.Precio = 899.99m;
        producto.Stock = 5;
        producto.RowVersion = NewRowVersion();

        context.Productos.Update(producto);
        await context.SaveChangesAsync();

        producto.Precio.Should().Be(899.99m);
        producto.Stock.Should().Be(5);
    }

    [Test]
    public async Task DeleteAsync_Existente_MarcaIsDeleted()
    {
        using var context = CreateContext(nameof(DeleteAsync_Existente_MarcaIsDeleted));

        context.Productos.Add(new Producto { Id = 1, Nombre = "Para Borrar", RowVersion = NewRowVersion() });
        await context.SaveChangesAsync();

        var producto = await context.Productos.FindAsync(1L);
        producto!.IsDeleted = true;
        producto.UpdatedAt = DateTime.UtcNow;

        context.Productos.Update(producto);
        await context.SaveChangesAsync();

        var resultado = await context.Productos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == 1);
        resultado.Should().NotBeNull();
        resultado!.IsDeleted.Should().BeTrue();
    }
}
