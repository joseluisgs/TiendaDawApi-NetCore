using Bunit;
using ClientBlazor.Cliente.Components.Shared;
using ClientBlazor.Cliente.State;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ClientBlazor.Tests.Components;

[TestFixture]
public class TokenDisplayTests
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
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void Should_Render_Input_Empty_Initially()
    {
        // Act
        var cut = _ctx.Render<TokenDisplay>();

        // Assert
        cut.Find("input").GetAttribute("value").Should().BeNullOrEmpty();
    }

    [Test]
    public void Should_Update_Input_When_Token_Changes_In_Store()
    {
        // Arrange
        var cut = _ctx.Render<TokenDisplay>();
        var testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fake.signature";

        // Act
        // InvokeAsync para ejecutar en el Dispatcher
        cut.InvokeAsync(() => _authStore.SetToken(testToken));

        // Assert
        cut.WaitForState(() => cut.Find("input").GetAttribute("value") == testToken);
        cut.Find("input").GetAttribute("value").Should().Be(testToken);
    }
    
    [Test]
    public void Should_Clear_Input_On_Logout()
    {
        // Arrange
        _authStore.SetToken("initial-token"); 
        
        var cut = _ctx.Render<TokenDisplay>();
        
        // Verificamos estado inicial
        cut.Find("input").GetAttribute("value").Should().Be("initial-token");

        // Act
        cut.InvokeAsync(() => _authStore.Logout());

        // Assert
        cut.WaitForState(() => string.IsNullOrEmpty(cut.Find("input").GetAttribute("value")));
        cut.Find("input").GetAttribute("value").Should().BeEmpty();
    }
}