using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Tests.Unit.Repositories.Productos;

public class ProductoRepositoryTests
{
    private TiendaDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TiendaDbContext(options);
    }

    private static byte[] NewRowVersion() => new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

    #region Constructor Tests

    [Test]
    public void Constructor_CreaInstanciaCorrectamente()
    {
        using var context = CreateContext(nameof(Constructor_CreaInstanciaCorrectamente));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();

        var repository = new ProductoRepository(context, loggerMock.Object);

        repository.Should().NotBeNull();
    }

    #endregion

    #region FindAllAsync Tests

    [Test]
    public async Task FindAllAsync_SinProductos_RetornaListaVacia()
    {
        using var context = CreateContext(nameof(FindAllAsync_SinProductos_RetornaListaVacia));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var result = await repository.FindAllAsync();

        result.Should().BeEmpty();
    }

    #endregion

    #region FindByIdAsync Tests

    [Test]
    public async Task FindByIdAsync_NoExistente_RetornaNull()
    {
        using var context = CreateContext(nameof(FindByIdAsync_NoExistente_RetornaNull));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var result = await repository.FindByIdAsync(999);

        result.Should().BeNull();
    }

    #endregion

    #region ExistsAsync Tests

    [Test]
    public async Task ExistsAsync_NoExistente_RetornaFalse()
    {
        using var context = CreateContext(nameof(ExistsAsync_NoExistente_RetornaFalse));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var result = await repository.ExistsAsync(999);

        result.Should().BeFalse();
    }

    #endregion

    #region SaveAsync Tests

    [Test]
    public async Task SaveAsync_NuevoProducto_AgregaALaDb()
    {
        using var context = CreateContext(nameof(SaveAsync_NuevoProducto_AgregaALaDb));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var producto = new Producto { Nombre = "Tablet", Precio = 299.99m, Stock = 20, RowVersion = NewRowVersion() };

        await repository.SaveAsync(producto);

        context.Productos.Should().HaveCount(1);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_NoExistente_NoLanzaExcepcion()
    {
        using var context = CreateContext(nameof(DeleteAsync_NoExistente_NoLanzaExcepcion));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var act = async () => await repository.DeleteAsync(999);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region FindAllPagedAsync Tests

    [Test]
    public async Task FindAllPagedAsync_PaginaVacia_RetornaElementosVacios()
    {
        using var context = CreateContext(nameof(FindAllPagedAsync_PaginaVacia_RetornaElementosVacios));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "P1", Precio = 100m, Stock = 10, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "P2", Precio = 200m, Stock = 20, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var filter = new ProductoFilterDto(null, null, null, null, null, 10, 10, "id", "asc");

        var (items, totalCount) = await repository.FindAllPagedAsync(filter);

        items.Should().BeEmpty();
        totalCount.Should().Be(2);
    }

    [Test]
    public void FindAllPagedAsync_MetodoExiste_EnElRepositorio()
    {
        using var context = CreateContext(nameof(FindAllPagedAsync_MetodoExiste_EnElRepositorio));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var methodInfo = typeof(ProductoRepository).GetMethod("FindAllPagedAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region FindByCategoriaIdAsync Tests

    [Test]
    public async Task FindByCategoriaIdAsync_SinProductos_RetornaVacio()
    {
        using var context = CreateContext(nameof(FindByCategoriaIdAsync_SinProductos_RetornaVacio));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var result = await repository.FindByCategoriaIdAsync(1);

        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public void UpdateAsync_MetodoExiste_EnElRepositorio()
    {
        using var context = CreateContext(nameof(UpdateAsync_MetodoExiste_EnElRepositorio));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var methodInfo = typeof(ProductoRepository).GetMethod("UpdateAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region DecrementStockAsync Tests

    [Test]
    public async Task DecrementStockAsync_ProductoNoExistente_RetornaFalse()
    {
        using var context = CreateContext(nameof(DecrementStockAsync_ProductoNoExistente_RetornaFalse));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var result = await repository.DecrementStockAsync(999, 1, NewRowVersion());

        result.Should().BeFalse();
    }

    #endregion

    #region GetRecentlyCreatedAsync Tests

    [Test]
    public async Task GetRecentlyCreatedAsync_SinProductos_RetornaVacio()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_SinProductos_RetornaVacio));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var result = await repository.GetRecentlyCreatedAsync(7);

        result.Should().BeEmpty();
    }

    #endregion

    #region BeginTransactionAsync Tests

    [Test]
    public void BeginTransactionAsync_MetodoExiste_EnElRepositorio()
    {
        using var context = CreateContext(nameof(BeginTransactionAsync_MetodoExiste_EnElRepositorio));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var methodInfo = typeof(ProductoRepository).GetMethod("BeginTransactionAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void ImplementsIProductoRepository()
    {
        using var context = CreateContext(nameof(ImplementsIProductoRepository));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        repository.Should().BeAssignableTo<IProductoRepository>();
    }

    #endregion

    #region FindAllAsNoTracking Tests

    [Test]
    public void FindAllAsNoTracking_SinProductos_RetornaQueryableVacio()
    {
        using var context = CreateContext(nameof(FindAllAsNoTracking_SinProductos_RetornaQueryableVacio));
        var loggerMock = new Mock<ILogger<ProductoRepository>>();
        var repository = new ProductoRepository(context, loggerMock.Object);

        var result = repository.FindAllAsNoTracking();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion
}
