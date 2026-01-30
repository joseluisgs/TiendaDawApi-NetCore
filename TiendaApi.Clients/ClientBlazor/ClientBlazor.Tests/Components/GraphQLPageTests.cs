using Bunit;
using ClientBlazor.Cliente.Components.Pages;
using ClientBlazor.Cliente.Services.GraphQL;
using ClientBlazor.Cliente.State.Notifications;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.Domain.Errors;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using CSharpFunctionalExtensions;

namespace ClientBlazor.Tests.Components;

[TestFixture]
public class GraphQLPageTests
{
    private BunitContext _ctx = null!;
    private Mock<IGraphQLService> _gqlServiceMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;
    private Mock<IAuthStore> _authStoreMock = null!;

    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _gqlServiceMock = new Mock<IGraphQLService>();
        _notificationStoreMock = new Mock<INotificationStore>();
        _authStoreMock = new Mock<IAuthStore>();

        _ctx.Services.AddSingleton(_gqlServiceMock.Object);
        _ctx.Services.AddSingleton(_notificationStoreMock.Object);
        _ctx.Services.AddSingleton(_authStoreMock.Object);
        
        _authStoreMock.Setup(s => s.IsAuthenticatedObservable).Returns(System.Reactive.Linq.Observable.Return(false));
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    [Test]
    public void GraphQLPage_Should_Render_Query_Section_By_Default()
    {
        // Act - Calificamos el nombre para evitar colisión con el namespace
        var cut = _ctx.Render<ClientBlazor.Cliente.Components.Pages.GraphQL>();

        // Assert
        cut.Find(".section-title").TextContent.Should().Be("GraphQL Query");
        cut.Find("textarea").GetAttribute("placeholder").Should().Contain("generara automaticamente");
    }

    [Test]
    public void Changing_To_Mutation_Should_Show_Warning()
    {
        // Act
        var cut = _ctx.Render<ClientBlazor.Cliente.Components.Pages.GraphQL>();
        cut.Find("select").Change("mutation");

        // Assert
        cut.Find("p").TextContent.Should().Contain("requieren autenticacion");
    }

    [Test]
    public async Task Clicking_Execute_Should_Call_Service()
    {
        // Arrange
        _gqlServiceMock.Setup(s => s.ExecuteQueryAsync<object>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Result.Success<object, DomainError>(new { data = "ok" }));
        
        var cut = _ctx.Render<ClientBlazor.Cliente.Components.Pages.GraphQL>();

        // Act
        await cut.InvokeAsync(() => cut.Find("button.btn-connect").Click());

        // Assert
        _gqlServiceMock.Verify(s => s.ExecuteQueryAsync<object>(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }
}
