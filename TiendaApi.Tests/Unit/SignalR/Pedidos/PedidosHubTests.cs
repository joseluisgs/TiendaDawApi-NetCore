using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Tests.Unit.SignalR.Pedidos;

public class PedidosHubTests
{
    private Mock<ILogger<PedidosHub>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<PedidosHub>>();
    }

    [Test]
    public void Constructor_CreaInstanciaCorrectamente()
    {
        var hub = new PedidosHub(_mockLogger.Object);
        hub.Should().NotBeNull();
    }

    [Test]
    public void PedidosHub_TieneAuthorize_RequiereAuth()
    {
        var attrs = typeof(PedidosHub).GetCustomAttributes(typeof(AuthorizeAttribute), true);
        attrs.Should().NotBeEmpty();
    }
}
