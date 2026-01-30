using Bunit;
using ClientBlazor.Cliente.Components.Shared;
using ClientBlazor.Cliente.State;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ClientBlazor.Tests.Components;

[TestFixture]
public class AuthPanelTests
{
    private BunitContext _ctx = null!;
    private AuthStore _authStore = null!;

    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _authStore = new AuthStore();
        _ctx.Services.AddSingleton(_authStore);
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    [Test]
    public void Should_Not_Render_Anything_When_Not_Authenticated()
    {
        // Act
        var cut = _ctx.Render<AuthPanel>();

        // Assert
        cut.FindAll(".auth-panel").Should().BeEmpty();
    }

    [Test]
    public void Should_Render_User_Info_When_Authenticated()
    {
        // Arrange
        _authStore.SetAuth("token", "kitty@test.com", "Hello Kitty", "USER");

        // Act
        var cut = _ctx.Render<AuthPanel>();

        // Assert
        cut.Find(".auth-greeting").TextContent.Should().Be("Hello Kitty");
        cut.Find(".detail-email").TextContent.Should().Be("kitty@test.com");
        cut.Find(".detail-role").TextContent.Should().Be("USER");
        cut.Find(".role-user").Should().NotBeNull();
    }

    [Test]
    public void Should_Render_Admin_Role_Class_Correctly()
    {
        // Arrange
        _authStore.SetAuth("token", "admin@test.com", "Admin", "ADMIN");

        // Act
        var cut = _ctx.Render<AuthPanel>();

        // Assert
        cut.Find(".role-admin").Should().NotBeNull();
    }

    [Test]
    public void Clicking_Logout_Should_Call_Store_Logout()
    {
        // Arrange
        _authStore.SetAuth("token", "user", "User", "USER");
        var cut = _ctx.Render<AuthPanel>();

        // Act
        cut.Find("button.auth-logout-btn").Click();

        // Assert
        _authStore.GetState().IsAuthenticated.Should().BeFalse();
        
        cut.WaitForState(() => cut.FindAll(".auth-panel").Count == 0);
    }
}
