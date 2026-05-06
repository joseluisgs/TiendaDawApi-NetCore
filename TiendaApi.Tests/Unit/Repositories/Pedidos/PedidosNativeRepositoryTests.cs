using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Repositories.Pedidos;

/// <summary>
/// Tests unitarios para PedidosNativeRepository.
/// Verifica el repositorio con driver nativo de MongoDB.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Repository")]
public class PedidosNativeRepositoryTests
{
    private Mock<IMongoDatabase> _mockDatabase = null!;
    private Mock<IMongoCollection<Pedido>> _mockCollection = null!;
    private Mock<ILogger<PedidosNativeRepository>> _mockLogger = null!;
    private PedidosNativeRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<Pedido>>();
        _mockLogger = new Mock<ILogger<PedidosNativeRepository>>();
        
        _mockDatabase.Setup(d => d.GetCollection<Pedido>("pedidos", null))
            .Returns(_mockCollection.Object);
            
        _repository = new PedidosNativeRepository(_mockDatabase.Object, _mockLogger.Object);
    }

    [Test]
    public async Task FindAllAsync_ConPedidos_RetornaListaOrdenada()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new() { Id = ObjectId.GenerateNewId(), UserId = 1, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new() { Id = ObjectId.GenerateNewId(), UserId = 1, CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };

        var mockCursor = new Mock<IAsyncCursor<Pedido>>();
        mockCursor.Setup(c => c.Current).Returns(pedidos);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<FindOptions<Pedido>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.FindAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAllAsync_SinPedidos_RetornaListaVacia()
    {
        // Arrange
        var mockCursor = new Mock<IAsyncCursor<Pedido>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Pedido>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<FindOptions<Pedido>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.FindAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task FindByIdAsync_ConIdExistente_RetornaPedido()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var pedido = new Pedido { Id = id, UserId = 1 };

        var mockCursor = new Mock<IAsyncCursor<Pedido>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Pedido> { pedido });
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<FindOptions<Pedido>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.FindByIdAsync(id.ToString());

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Test]
    public async Task FindByIdAsync_ConIdNoExistente_RetornaNull()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();

        var mockCursor = new Mock<IAsyncCursor<Pedido>>();
        mockCursor.Setup(c => c.Current).Returns(new List<Pedido>());
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<FindOptions<Pedido>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.FindByIdAsync(id.ToString());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FindByIdAsync_ConIdInvalido_RetornaNull()
    {
        // Act
        var result = await _repository.FindByIdAsync("id-invalido");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FindByUserIdAsync_ConUserId_RetornaPedidosDelUsuario()
    {
        // Arrange
        long userId = 1;
        var pedidos = new List<Pedido>
        {
            new() { Id = ObjectId.GenerateNewId(), UserId = userId },
            new() { Id = ObjectId.GenerateNewId(), UserId = userId }
        };

        var mockCursor = new Mock<IAsyncCursor<Pedido>>();
        mockCursor.Setup(c => c.Current).Returns(pedidos);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<FindOptions<Pedido>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.FindByUserIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.UserId.Should().Be(userId));
    }

    [Test]
    public async Task SaveAsync_InsertarPedido_RetornaPedidoConId()
    {
        // Arrange
        var pedido = new Pedido { UserId = 1 };

        _mockCollection.Setup(c => c.InsertOneAsync(
            It.IsAny<Pedido>(),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _repository.SaveAsync(pedido);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
    }

    [Test]
    public async Task UpdateAsync_ActualizarPedido_RetornaPedido()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();
        var pedido = new Pedido { Id = id, UserId = 1 };

        _mockCollection.Setup(c => c.ReplaceOneAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<Pedido>(),
            It.IsAny<ReplaceOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));

        // Act
        var result = await _repository.UpdateAsync(pedido);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
    }

    [Test]
    public async Task FindByUserIdPagedAsync_ConPaginacion_RetornaPedidosYPagina()
    {
        // Arrange
        long userId = 1;
        var pedidos = new List<Pedido>
        {
            new() { Id = ObjectId.GenerateNewId(), UserId = userId }
        };

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<CountOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var mockCursor = new Mock<IAsyncCursor<Pedido>>();
        mockCursor.Setup(c => c.Current).Returns(pedidos);
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<Pedido>>(),
            It.IsAny<FindOptions<Pedido>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.FindByUserIdPagedAsync(userId, 0, 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(10);
    }
}
