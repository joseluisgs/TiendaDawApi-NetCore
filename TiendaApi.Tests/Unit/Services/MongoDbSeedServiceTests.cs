using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using TiendaApi.Api.Data.Seed.Mongo;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Services;

/// <summary>
///     Tests unitarios para MongoDbSeeder.
/// </summary>
public class MongoDbSeederTests {
    private Mock<ILogger<MongoDbSeeder>> _mockLogger = null!;
    private Mock<IMongoCollection<Pedido>> _mockPedidosCollection = null!;
    private MongoDbSeeder _seedService = null!;

    [SetUp]
    public void Setup() {
        _mockPedidosCollection = new Mock<IMongoCollection<Pedido>>();
        _mockLogger = new Mock<ILogger<MongoDbSeeder>>();
        _seedService = new MongoDbSeeder(_mockPedidosCollection.Object, _mockLogger.Object);
    }

    [Test]
    public async Task SeedAsync_ColeccionVacia_InsertaPedidos() {
        long count = 0L;
        _mockPedidosCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Pedido>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        _mockPedidosCollection.Setup(x => x.InsertManyAsync(
                It.IsAny<IEnumerable<Pedido>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _seedService.SeedAsync();

        await act.Should().NotThrowAsync();

        _mockPedidosCollection.Verify(
            x => x.InsertManyAsync(
                It.IsAny<IEnumerable<Pedido>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SeedAsync_ColeccionConDatos_NoInsertaNada() {
        long count = 5L;
        _mockPedidosCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Pedido>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        Func<Task> act = async () => await _seedService.SeedAsync();

        await act.Should().NotThrowAsync();

        _mockPedidosCollection.Verify(
            x => x.InsertManyAsync(
                It.IsAny<IEnumerable<Pedido>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("ya contiene")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    [Test]
    public async Task SeedAsync_ErrorMongoDB_NoLanzaExcepcion() {
        long count = 0L;
        _mockPedidosCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Pedido>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        _mockPedidosCollection.Setup(x => x.InsertManyAsync(
                It.IsAny<IEnumerable<Pedido>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MongoDB connection error"));

        Func<Task> act = async () => await _seedService.SeedAsync();

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Error")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    [Test]
    public async Task SeedAsync_ColeccionVacia_LogueaSembrado() {
        long count = 0L;
        _mockPedidosCollection.Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Pedido>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        _mockPedidosCollection.Setup(x => x.InsertManyAsync(
                It.IsAny<IEnumerable<Pedido>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _seedService.SeedAsync();

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Sembrando")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Insertados")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }
}
