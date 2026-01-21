using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Tests.Unit.Realtime;

public class ProductosHubTests
{
    private Mock<ILogger<ProductosHub>> _loggerMock = null!;
    private HubCallerContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ProductosHub>>();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "testuser")
        }, "TestAuthType");

        _context = Mock.Of<HubCallerContext>(c => 
            c.ConnectionId == "test-connection-id" && 
            c.User == new ClaimsPrincipal(identity));
    }

    #region Constructor Tests

    [Test]
    public void Constructor_CreaInstanciaCorrectamente()
    {
        var hub = new ProductosHub(_loggerMock.Object);
        hub.Should().NotBeNull();
    }

    #endregion

    #region Attribute Tests

    [Test]
    public void ProductosHub_TieneAllowAnonymousAttribute_Clase()
    {
        var attribute = typeof(ProductosHub)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
            .FirstOrDefault();

        attribute.Should().NotBeNull("La clase ProductosHub debe tener [AllowAnonymous]");
    }

    [Test]
    public void ProductosHub_NoTieneAuthorizeAttribute_Clase()
    {
        var attribute = typeof(ProductosHub)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .FirstOrDefault();

        attribute.Should().BeNull("La clase ProductosHub NO debe tener [Authorize]");
    }

    #endregion

    #region OnConnectedAsync Tests

    [Test]
    public async Task OnConnectedAsync_ClienteAnonimo_LogueaInformacion()
    {
        var hub = new ProductosHub(_loggerMock.Object);
        SetupHubContext(hub, _context);

        await hub.OnConnectedAsync();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cliente conectado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_MultiplesClientes_CreaMultiplesConexiones()
    {
        var hub1 = new ProductosHub(_loggerMock.Object);
        var hub2 = new ProductosHub(_loggerMock.Object);

        var context1 = Mock.Of<HubCallerContext>(c => 
            c.ConnectionId == "connection-1" && 
            c.User == new ClaimsPrincipal(new ClaimsIdentity()));
        var context2 = Mock.Of<HubCallerContext>(c => 
            c.ConnectionId == "connection-2" && 
            c.User == new ClaimsPrincipal(new ClaimsIdentity()));

        SetupHubContext(hub1, context1);
        SetupHubContext(hub2, context2);

        await hub1.OnConnectedAsync();
        await hub2.OnConnectedAsync();

        hub1.Should().NotBeNull();
        hub2.Should().NotBeNull();
    }

    #endregion

    #region OnDisconnectedAsync Tests

    [Test]
    public async Task OnDisconnectedAsync_SinError_LogueaInformacion()
    {
        var hub = new ProductosHub(_loggerMock.Object);
        SetupHubContext(hub, _context);

        await hub.OnDisconnectedAsync(null);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cliente desconectado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task OnDisconnectedAsync_ConError_LogueaWarning()
    {
        var hub = new ProductosHub(_loggerMock.Object);
        SetupHubContext(hub, _context);
        var exception = new Exception("Connection lost");

        await hub.OnDisconnectedAsync(exception);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cliente desconectado")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task OnDisconnectedAsync_ConErrorNulo_NoLanzaExcepcion()
    {
        var hub = new ProductosHub(_loggerMock.Object);
        SetupHubContext(hub, _context);

        var act = async () => await hub.OnDisconnectedAsync(null);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task OnDisconnectedAsync_ConExcepcionReal_NoLanzaExcepcion()
    {
        var hub = new ProductosHub(_loggerMock.Object);
        SetupHubContext(hub, _context);
        var exception = new Exception("Test exception");

        var act = async () => await hub.OnDisconnectedAsync(exception);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private void SetupHubContext(ProductosHub hub, HubCallerContext context)
    {
        var field = typeof(Hub).GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(hub, context);
    }

    #endregion
}
