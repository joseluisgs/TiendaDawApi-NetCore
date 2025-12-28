using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.WebSockets;

namespace TiendaApi.Tests;

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
        var productoId = 1L;
        var productoNombre = "Test Product";

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoCreatedAsync(productoId, productoNombre))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyProductoUpdatedAsync_WithNoConnections_ShouldNotThrow()
    {
        // Arrange
        var productoId = 1L;
        var productoNombre = "Test Product";

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoUpdatedAsync(productoId, productoNombre))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyProductoDeletedAsync_WithNoConnections_ShouldNotThrow()
    {
        // Arrange
        var productoId = 1L;
        var productoNombre = "Test Product";

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoDeletedAsync(productoId, productoNombre))
            .Should().NotThrowAsync();
    }
}
