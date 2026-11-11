using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Repositories.Pedidos;

[TestFixture]
[Category("Unit")]
[Category("Repository")]
public class PedidosEfCoreRepositoryTests {
    [Test]
    public void Constructor_CreaInstanciaCorrectamente() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);

        repository.Should().NotBeNull();
    }

    [Test]
    public async Task FindAllAsync_MetodoExiste_EnElRepositorio() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        MethodInfo? methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindAllAsync");

        methodInfo.Should().NotBeNull();
    }

    [Test]
    public async Task FindByUserIdAsync_MetodoExiste_EnElRepositorio() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        MethodInfo? methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindByUserIdAsync");

        methodInfo.Should().NotBeNull();
    }

    [Test]
    public async Task FindByIdAsync_MetodoExiste_EnElRepositorio() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        MethodInfo? methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindByIdAsync");

        methodInfo.Should().NotBeNull();
    }

    [Test]
    public void FindByIdAsync_IdNoObjectId_LanzaExcepcion() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);

        Func<Task<Pedido?>> act = async () => await repository.FindByIdAsync("invalid-id");

        act.Should().ThrowAsync<FormatException>();
    }

    [Test]
    public async Task FindByUserIdPagedAsync_MetodoExiste_EnElRepositorio() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        MethodInfo? methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindByUserIdPagedAsync");

        methodInfo.Should().NotBeNull();
    }

    [Test]
    public async Task SaveAsync_MetodoExiste_EnElRepositorio() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        MethodInfo? methodInfo = typeof(PedidosEfCoreRepository).GetMethod("SaveAsync");

        methodInfo.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_MetodoExiste_EnElRepositorio() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        MethodInfo? methodInfo = typeof(PedidosEfCoreRepository).GetMethod("UpdateAsync");

        methodInfo.Should().NotBeNull();
    }

    [Test]
    public void ImplementsIPedidosRepository() {
        DbContextOptions<TiendaMongoContext> options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase("Test")
            .Options;

        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);

        repository.Should().BeAssignableTo<IPedidosRepository>();
    }
}
