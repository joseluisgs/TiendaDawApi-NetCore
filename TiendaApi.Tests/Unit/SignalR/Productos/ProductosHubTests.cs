using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TiendaApi.Apis.Realtime.Pedidos;
using TiendaApi.Apis.Realtime.Productos;

namespace TiendaApi.Tests.Unit.SignalR.Productos;

public class ProductosHubTests
{
    private Mock<ILogger<ProductosHub>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ProductosHub>>();
    }

    [Test]
    public void Constructor_CreaInstanciaCorrectamente()
    {
        var hub = new ProductosHub(_mockLogger.Object);
        hub.Should().NotBeNull();
    }

    [Test]
    public void ProductosHub_NoTieneAuthorize_Publico()
    {
        var attrs = typeof(ProductosHub).GetCustomAttributes(typeof(AllowAnonymousAttribute), true);
        attrs.Should().NotBeEmpty();
    }
}
