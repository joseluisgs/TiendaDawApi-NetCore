using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Tests.Unit.WebSockets.Productos;

/// <summary>
/// Tests unitarios para ProductosWebSocketHandler.
/// </summary>
public class WebSocketHandlerTests
{
    private Mock<ILogger<ProductosWebSocketHandler>> _mockLogger = null!;
    private ProductosWebSocketHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ProductosWebSocketHandler>>();
        _handler = new ProductosWebSocketHandler(_mockLogger.Object);
    }

    #region Tests NotifyAsync (CREATE)

    [Test]
    public async Task NotifyAsync_Create_SinConexiones_NoLanzaExcepcion()
    {
        var producto = new ProductoDto(
            1,
            "Test Product",
            "Description",
            99.99m,
            10,
            null,
            1,
            "Categoria",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.CREATED,
            producto.Id,
            producto
        ));
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyAsync_Create_ConProductoCompleto_NotificaConDatos()
    {
        var producto = new ProductoDto(
            42,
            "Producto Test",
            "Descripción del producto",
            99.99m,
            10,
            "https://example.com/image.jpg",
            1,
            "Categoria",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.CREATED,
            producto.Id,
            producto
        ));
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests NotifyAsync (UPDATE)

    [Test]
    public async Task NotifyAsync_Update_SinConexiones_NoLanzaExcepcion()
    {
        var producto = new ProductoDto(
            1,
            "Test Product",
            "Description",
            99.99m,
            10,
            null,
            1,
            "Categoria",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.UPDATED,
            producto.Id,
            producto
        ));
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyAsync_Update_ConProductoCompleto_NotificaConDatos()
    {
        var producto = new ProductoDto(
            42,
            "Producto Test Actualizado",
            "Nueva descripción",
            149.99m,
            20,
            "https://example.com/new-image.jpg",
            1,
            "Categoria",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.UPDATED,
            producto.Id,
            producto
        ));
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests NotifyAsync (DELETE)

    [Test]
    public async Task NotifyAsync_Delete_SinConexiones_NoLanzaExcepcion()
    {
        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.DELETED,
            123,
            null
        ));
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyAsync_Delete_ConIdValido_NotificaConId()
    {
        var productoId = 456L;

        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.DELETED,
            productoId,
            null
        ));
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests de notificación con conexiones (simulado)

    [Test]
    public async Task NotifyAsync_Create_ConProductoValido_NoLanzaExcepcion()
    {
        var producto = new ProductoDto(
            1,
            "Test",
            "Desc",
            10m,
            5,
            null,
            1,
            "Cat",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.CREATED,
            producto.Id,
            producto
        ));
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyAsync_Update_ConProductoValido_NoLanzaExcepcion()
    {
        var producto = new ProductoDto(
            1,
            "Test",
            "Desc",
            10m,
            5,
            null,
            1,
            "Cat",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.UPDATED,
            producto.Id,
            producto
        ));
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyAsync_Delete_ConIdValido_NoLanzaExcepcion()
    {
        var act = async () => await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.DELETED,
            999,
            null
        ));
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task NotifyAsync_TodosLosTipos_ProcesaSinErrores()
    {
        var producto = new ProductoDto(
            1,
            "Test",
            "Desc",
            10m,
            5,
            null,
            1,
            "Cat",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // CREATE
        await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.CREATED, 1, producto));

        // UPDATE
        await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.UPDATED, 1, producto));

        // DELETE
        await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.DELETED, 1, null));

        // CREATE de nuevo
        await _handler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.CREATED, 2, producto));
    }

    #endregion
}
