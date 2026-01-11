using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Dtos.Productos;
using TiendaApi.WebSockets.Productos;

namespace TiendaApi.Tests.Unit.WebSockets.Productos;

/// <summary>
/// Smoke tests for WebSocket handler
/// </summary>
public class WebSocketHandlerTests
{
    private ProductoWebSocketHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var mockLogger = new Mock<ILogger<ProductoWebSocketHandler>>();
        _handler = new ProductoWebSocketHandler(mockLogger.Object);
    }

    [Test]
    public async Task NotifyProductoCreatedAsync_WithNoConnections_ShouldNotThrow()
    {
        // Arrange
        var producto = new ProductoDto { Id = 1, Nombre = "Test Product" };

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoCreatedAsync(producto))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyProductoUpdatedAsync_WithNoConnections_ShouldNotThrow()
    {
        // Arrange
        var producto = new ProductoDto { Id = 1, Nombre = "Test Product" };

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoUpdatedAsync(producto))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyProductoDeletedAsync_WithNoConnections_ShouldNotThrow()
    {
        // Arrange
        var productoId = 1L;

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoDeletedAsync(productoId))
            .Should().NotThrowAsync();
    }
}
