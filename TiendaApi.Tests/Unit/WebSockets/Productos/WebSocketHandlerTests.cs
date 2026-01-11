using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Dtos.Productos;
using TiendaApi.WebSockets.Productos;

namespace TiendaApi.Tests.Unit.WebSockets.Productos;

/// <summary>
/// Tests de smoke para el manejador de WebSocket
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
    public async Task NotifyProductoCreatedAsync_SinConexiones_NoLanzaExcepcion()
    {
        // Arrange
        var producto = new ProductoDto { Id = 1, Nombre = "Test Product" };

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoCreatedAsync(producto))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyProductoUpdatedAsync_SinConexiones_NoLanzaExcepcion()
    {
        // Arrange
        var producto = new ProductoDto { Id = 1, Nombre = "Test Product" };

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoUpdatedAsync(producto))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyProductoDeletedAsync_SinConexiones_NoLanzaExcepcion()
    {
        // Arrange
        var productoId = 1L;

        // Act & Assert
        await _handler.Invoking(h => h.NotifyProductoDeletedAsync(productoId))
            .Should().NotThrowAsync();
    }
}
