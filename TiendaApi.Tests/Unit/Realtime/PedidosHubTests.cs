using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Tests.Unit.Realtime;

/// <summary>
/// Tests unitarios para SignalR Hubs (PedidosHub).
/// </summary>
public class PedidosHubTests
{
    private Mock<ILogger<PedidosHub>> _loggerMock = null!;
    private Mock<IGroupManager> _groupManagerMock = null!;
    private ClaimsPrincipal _userAdmin = null!;
    private ClaimsPrincipal _userRegular = null!;
    private HubCallerContext _contextAdmin = null!;
    private HubCallerContext _contextRegular = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PedidosHub>>();
        _groupManagerMock = new Mock<IGroupManager>();

        // Crear usuario Admin
        var adminIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuthType");

        _userAdmin = new ClaimsPrincipal(adminIdentity);

        // Crear usuario Regular
        var regularIdentity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Name, "userdaw"),
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuthType");

        _userRegular = new ClaimsPrincipal(regularIdentity);

        // Mock del contexto para Admin
        _contextAdmin = Mock.Of<HubCallerContext>(c => 
            c.ConnectionId == "admin-connection-id" && 
            c.User == _userAdmin);

        // Mock del contexto para User Regular
        _contextRegular = Mock.Of<HubCallerContext>(c => 
            c.ConnectionId == "user-connection-id" && 
            c.User == _userRegular);
    }

    #region OnConnectedAsync Tests

    [Test]
    public async Task OnConnectedAsync_AdminUser_SeAñadeAGruposUserYAdmin()
    {
        // Arrange
        var hub = new PedidosHub(_loggerMock.Object);
        
        SetupHubContext(hub, _contextAdmin);
        _groupManagerMock
            .Setup(g => g.AddToGroupAsync("admin-connection-id", "user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _groupManagerMock
            .Setup(g => g.AddToGroupAsync("admin-connection-id", "admins", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync("admin-connection-id", "user-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync("admin-connection-id", "admins", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_RegularUser_SeAñadeSoloAGrupoUser()
    {
        // Arrange
        var hub = new PedidosHub(_loggerMock.Object);
        SetupHubContext(hub, _contextRegular);
        _groupManagerMock
            .Setup(g => g.AddToGroupAsync("user-connection-id", "user-2", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync("user-connection-id", "user-2", It.IsAny<CancellationToken>()),
            Times.Once);
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync("user-connection-id", "admins", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_UserSinId_NoSeAñadeAGrupo()
    {
        // Arrange
        var userWithoutId = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "anonymous")
        }, "TestAuthType"));

        var contextWithoutId = Mock.Of<HubCallerContext>(c => 
            c.ConnectionId == "anonymous-connection-id" && 
            c.User == userWithoutId);

        var hub = new PedidosHub(_loggerMock.Object);
        SetupHubContext(hub, contextWithoutId);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        _groupManagerMock.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region OnDisconnectedAsync Tests

    [Test]
    public async Task OnDisconnectedAsync_SinError_NoLanzaExcepcion()
    {
        // Arrange
        var hub = new PedidosHub(_loggerMock.Object);
        SetupHubContext(hub, _contextAdmin);

        // Act & Assert
        async Task Act() => await hub.OnDisconnectedAsync(null);
        
        // Should not throw
        await Act();
    }

    [Test]
    public async Task OnDisconnectedAsync_ConError_NoLanzaExcepcion()
    {
        // Arrange
        var hub = new PedidosHub(_loggerMock.Object);
        SetupHubContext(hub, _contextAdmin);
        var exception = new Exception("Connection lost");

        // Act & Assert
        async Task Act() => await hub.OnDisconnectedAsync(exception);
        
        // Should not throw
        await Act();
    }

    #endregion

    #region GetConnectionInfo Tests

    [Test]
    public void GetConnectionInfo_AdminUser_RetornaInformacionCorrecta()
    {
        // Arrange
        var hub = new PedidosHub(_loggerMock.Object);
        SetupHubContext(hub, _contextAdmin);

        // Act
        var result = hub.GetConnectionInfo();

        // Assert
        result.Should().NotBeNull();
        
        var connectionId = result.GetType().GetProperty("connectionId")?.GetValue(result) as string;
        var userId = result.GetType().GetProperty("userId")?.GetValue(result) as string;
        var userName = result.GetType().GetProperty("userName")?.GetValue(result) as string;
        var isAdmin = result.GetType().GetProperty("isAdmin")?.GetValue(result) as bool?;
        
        connectionId.Should().Be("admin-connection-id");
        userId.Should().Be("1");
        userName.Should().Be("admin");
        isAdmin.Should().BeTrue();
    }

    [Test]
    public void GetConnectionInfo_RegularUser_RetornaInformacionCorrecta()
    {
        // Arrange
        var hub = new PedidosHub(_loggerMock.Object);
        SetupHubContext(hub, _contextRegular);

        // Act
        var result = hub.GetConnectionInfo();

        // Assert
        result.Should().NotBeNull();
        
        var connectionId = result.GetType().GetProperty("connectionId")?.GetValue(result) as string;
        var userId = result.GetType().GetProperty("userId")?.GetValue(result) as string;
        var userName = result.GetType().GetProperty("userName")?.GetValue(result) as string;
        var isAdmin = result.GetType().GetProperty("isAdmin")?.GetValue(result) as bool?;
        
        connectionId.Should().Be("user-connection-id");
        userId.Should().Be("2");
        userName.Should().Be("userdaw");
        isAdmin.Should().BeFalse();
    }

    #endregion

    #region Attribute Tests

    [Test]
    public void PedidosHub_TieneAuthorizeAttribute_Clase()
    {
        // Arrange & Act
        var attribute = typeof(PedidosHub)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .FirstOrDefault();

        // Assert
        attribute.Should().NotBeNull("La clase PedidosHub debe tener [Authorize]");
    }

    [Test]
    public void GetConnectionInfo_TieneAuthorizeAttribute_Metodo()
    {
        // Arrange & Act
        var methodInfo = typeof(PedidosHub).GetMethod("GetConnectionInfo");
        var attribute = methodInfo?
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .FirstOrDefault();

        // Assert
        attribute.Should().NotBeNull("El método GetConnectionInfo debe tener [Authorize]");
    }

    #endregion

    #region Helper Methods

    private void SetupHubContext(PedidosHub hub, HubCallerContext context)
    {
        var field = typeof(Hub).GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(hub, context);

        var groupsField = typeof(Hub).GetField("_groups", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        groupsField?.SetValue(hub, _groupManagerMock.Object);
    }

    #endregion
}
