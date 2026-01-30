using Bunit;
using ClientBlazor.Cliente.Components.Pages;
using ClientBlazor.Cliente.Services.SignalR;
using ClientBlazor.Cliente.State.Notifications;
using ClientBlazor.Cliente.State.Auth;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace ClientBlazor.Tests.Components;

[TestFixture]
public class SignalRPageTests
{
    private BunitContext _ctx = null!;
    private Mock<ISignalRService> _signalRServiceMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;
    private Mock<IAuthStore> _authStoreMock = null!;

    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _signalRServiceMock = new Mock<ISignalRService>();
        _notificationStoreMock = new Mock<INotificationStore>();
        _authStoreMock = new Mock<IAuthStore>();

        _ctx.Services.AddSingleton(_signalRServiceMock.Object);
        _ctx.Services.AddSingleton(_notificationStoreMock.Object);
        _ctx.Services.AddSingleton(_authStoreMock.Object);
        
        _authStoreMock.Setup(s => s.IsAuthenticatedObservable).Returns(System.Reactive.Linq.Observable.Return(false));
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    [Test]
    public void Connect_Button_Should_Reflect_Service_State()
    {
        // Arrange - Desconectado
        _signalRServiceMock.Setup(s => s.IsConnected).Returns(false);
        var cut = _ctx.Render<SignalR>();

        // Assert - Debería decir "Conectar Hub"
        cut.Find("button.btn-quick").TextContent.Should().Contain("Conectar");

        // Act - Simular Conectado
        _signalRServiceMock.Setup(s => s.IsConnected).Returns(true);
        cut.Render(); // Re-renderizar

        // Assert - Debería decir "Hub Activo"
        cut.Find("button.btn-quick").TextContent.Should().Contain("Activo");
    }
}
