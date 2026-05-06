using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Data;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Repositories.Pedidos;

[TestFixture]
[Category("Unit")]
[Category("Repository")]
public class PedidosEfCoreRepositoryTests
{
    #region Constructor Tests

    [Test]
    public void Constructor_CreaInstanciaCorrectamente()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);

        repository.Should().NotBeNull();
    }

    #endregion

    #region FindAllAsync Tests

    [Test]
    public async Task FindAllAsync_MetodoExiste_EnElRepositorio()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        var methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindAllAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region FindByUserIdAsync Tests

    [Test]
    public async Task FindByUserIdAsync_MetodoExiste_EnElRepositorio()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        var methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindByUserIdAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region FindByIdAsync Tests

    [Test]
    public async Task FindByIdAsync_MetodoExiste_EnElRepositorio()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        var methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindByIdAsync");

        methodInfo.Should().NotBeNull();
    }

    [Test]
    public void FindByIdAsync_IdNoObjectId_LanzaExcepcion()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);

        var act = async () => await repository.FindByIdAsync("invalid-id");

        act.Should().ThrowAsync<FormatException>();
    }

    #endregion

    #region FindByUserIdPagedAsync Tests

    [Test]
    public async Task FindByUserIdPagedAsync_MetodoExiste_EnElRepositorio()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        var methodInfo = typeof(PedidosEfCoreRepository).GetMethod("FindByUserIdPagedAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region SaveAsync Tests

    [Test]
    public async Task SaveAsync_MetodoExiste_EnElRepositorio()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        var methodInfo = typeof(PedidosEfCoreRepository).GetMethod("SaveAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_MetodoExiste_EnElRepositorio()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);
        var methodInfo = typeof(PedidosEfCoreRepository).GetMethod("UpdateAsync");

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void ImplementsIPedidosRepository()
    {
        var options = new DbContextOptionsBuilder<TiendaMongoContext>()
            .UseInMemoryDatabase(databaseName: "Test")
            .Options;
        
        using var context = new TiendaMongoContext(options);
        var loggerMock = new Mock<ILogger<PedidosEfCoreRepository>>();

        var repository = new PedidosEfCoreRepository(context, loggerMock.Object);

        repository.Should().BeAssignableTo<IPedidosRepository>();
    }

    #endregion
}