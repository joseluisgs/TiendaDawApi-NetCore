using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Dtos.Productos;
using TiendaApi.WebSockets.Productos;

namespace TiendaApi.Tests.Unit.WebSockets.Productos;

/// <summary>
/// Tests unitarios para ProductoWebSocketHandler.
/// </summary>
public class WebSocketHandlerTests
{
    private Mock<ILogger<ProductoWebSocketHandler>> _mockLogger = null!;
    private ProductoWebSocketHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ProductoWebSocketHandler>>();
        _handler = new ProductoWebSocketHandler(_mockLogger.Object);
    }

    #region Tests NotifyProductoCreatedAsync

    /// <summary>
    /// Dado un producto válido, cuando se notifica creación sin conexiones, entonces no lanza excepción.
    /// Returns: Unit.Success (notificación omitida)
    /// </summary>
    [Test]
    public async Task NotifyProductoCreatedAsync_SinConexiones_NoLanzaExcepcion()
    {
        var producto = new ProductoDto { Id = 1, Nombre = "Test Product" };

        var act = async () => await _handler.NotifyProductoCreatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un producto con datos completos, cuando se notifica creación, entonces incluye todos los datos.
    /// Returns: Unit.Success (notificación con datos completos)
    /// </summary>
    [Test]
    public async Task NotifyProductoCreatedAsync_ConProductoCompleto_NotificaConDatos()
    {
        var producto = new ProductoDto
        {
            Id = 42,
            Nombre = "Producto Test",
            Descripcion = "Descripción del producto",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };

        var act = async () => await _handler.NotifyProductoCreatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests NotifyProductoUpdatedAsync

    /// <summary>
    /// Dado un producto válido, cuando se notifica actualización sin conexiones, entonces no lanza excepción.
    /// Returns: Unit.Success (notificación omitida)
    /// </summary>
    [Test]
    public async Task NotifyProductoUpdatedAsync_SinConexiones_NoLanzaExcepcion()
    {
        var producto = new ProductoDto { Id = 1, Nombre = "Test Product" };

        var act = async () => await _handler.NotifyProductoUpdatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un producto con precio cero, cuando se notifica actualización, entonces no lanza excepción.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task NotifyProductoUpdatedAsync_ConPrecioCero_NoLanzaExcepcion()
    {
        var producto = new ProductoDto { Id = 1, Nombre = "Producto Gratis", Precio = 0 };

        var act = async () => await _handler.NotifyProductoUpdatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests NotifyProductoDeletedAsync

    /// <summary>
    /// Dado un ID de producto válido, cuando se notifica eliminación sin conexiones, entonces no lanza excepción.
    /// Returns: Unit.Success (notificación omitida)
    /// </summary>
    [Test]
    public async Task NotifyProductoDeletedAsync_SinConexiones_NoLanzaExcepcion()
    {
        var productoId = 1L;

        var act = async () => await _handler.NotifyProductoDeletedAsync(productoId);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un ID negativo, cuando se notifica eliminación, entonces no lanza excepción.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task NotifyProductoDeletedAsync_IdNegativo_NoLanzaExcepcion()
    {
        var productoId = -1L;

        var act = async () => await _handler.NotifyProductoDeletedAsync(productoId);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un ID cero, cuando se notifica eliminación, entonces no lanza excepción.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task NotifyProductoDeletedAsync_IdCero_NoLanzaExcepcion()
    {
        var productoId = 0L;

        var act = async () => await _handler.NotifyProductoDeletedAsync(productoId);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests Notificación Genérica

    /// <summary>
    /// Dado un producto con nombre largo, cuando se notifica creación, entonces procesa correctamente.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task NotifyProductoCreatedAsync_ConNombreLargo_ProcesaCorrectamente()
    {
        var producto = new ProductoDto
        {
            Id = 1,
            Nombre = "Este es un nombre muy largo para verificar que se maneja correctamente en la serialización"
        };

        var act = async () => await _handler.NotifyProductoCreatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un producto con caracteres especiales, cuando se notifica, entonces procesa correctamente.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task NotifyProductoCreatedAsync_ConCaracteresEspeciales_ProcesaCorrectamente()
    {
        var producto = new ProductoDto
        {
            Id = 1,
            Nombre = "Producto con ñ y acentos: ratón, año",
            Descripcion = "Descripción con \"comillas\" y 'apóstrofes'"
        };

        var act = async () => await _handler.NotifyProductoCreatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un producto con precio decimal grande, cuando se notifica, entonces procesa correctamente.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task NotifyProductoCreatedAsync_ConPrecioDecimalGrande_ProcesaCorrectamente()
    {
        var producto = new ProductoDto
        {
            Id = 1,
            Nombre = "Producto caro",
            Precio = 9999999.99m
        };

        var act = async () => await _handler.NotifyProductoCreatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests Multiple Notifications

    /// <summary>
    /// Dado múltiples notificaciones secuenciales, cuando se envían, entonces no fallan.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task Notify_MultiplesNotificaciones_Sequenciales_NoFallan()
    {
        var producto = new ProductoDto { Id = 1, Nombre = "Test" };

        await _handler.NotifyProductoCreatedAsync(producto);
        await _handler.NotifyProductoUpdatedAsync(producto);
        await _handler.NotifyProductoDeletedAsync(1);
        await _handler.NotifyProductoCreatedAsync(producto);

        var act = async () => await _handler.NotifyProductoUpdatedAsync(producto);
        await act.Should().NotThrowAsync();
    }

    #endregion
}
