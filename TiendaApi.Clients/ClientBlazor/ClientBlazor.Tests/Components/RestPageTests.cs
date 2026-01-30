using Bunit;
using ClientBlazor.Cliente.Components.Pages;
using ClientBlazor.Cliente.Services.Rest;
using ClientBlazor.Cliente.State.Notifications;
using ClientBlazor.Cliente.DTOs.Common;
using ClientBlazor.Cliente.DTOs.Productos;
using ClientBlazor.Cliente.Domain.Errors;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using CSharpFunctionalExtensions;

namespace ClientBlazor.Tests.Components;

[TestFixture]
public class RestPageTests
{
    private BunitContext _ctx = null!;
    private Mock<IRestService> _restServiceMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;

    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _restServiceMock = new Mock<IRestService>();
        _notificationStoreMock = new Mock<INotificationStore>();

        _ctx.Services.AddSingleton(_restServiceMock.Object);
        _ctx.Services.AddSingleton(_notificationStoreMock.Object);
        
        // Mock necesario para NavMenu que está dentro de Rest
        var authStoreMock = new Mock<ClientBlazor.Cliente.State.Auth.IAuthStore>();
        authStoreMock.Setup(s => s.IsAuthenticatedObservable).Returns(System.Reactive.Linq.Observable.Return(false));
        _ctx.Services.AddSingleton(authStoreMock.Object);
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    [Test]
    public void RestPage_Should_Render_Initial_Controls()
    {
        // Act
        var cut = _ctx.Render<Rest>();

        // Assert
        cut.Find("h3.section-title").TextContent.Should().Be("REST API Client");
        cut.Find("select").Should().NotBeNull();
        cut.Find("button.btn-connect").TextContent.Trim().Should().Be("Ejecutar");
    }

    [Test]
    public void Selecting_Post_Should_Show_Json_Editor()
    {
        // Act
        var cut = _ctx.Render<Rest>();
        
        // Buscamos el segundo select (operacion) y cambiamos a 'post'
        var selects = cut.FindAll("select");
        selects[1].Change("post");

        // Assert
        cut.Find(".editor-section").Should().NotBeNull();
        cut.Find("textarea").Should().NotBeNull();
    }
}
