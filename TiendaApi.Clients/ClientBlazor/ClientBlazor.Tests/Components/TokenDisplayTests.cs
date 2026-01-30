using Bunit;
using ClientBlazor.Cliente.Components.Shared;
using ClientBlazor.Cliente.State.Auth;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ClientBlazor.Tests.Components;

/// <summary>
/// Pruebas para el componente de visualización del token JWT.
/// Objetivo: Validar la reactividad de la UI ante cambios en el token de sesión.
/// </summary>
[TestFixture]
public class TokenDisplayTests
{
    private BunitContext _ctx = null!;
    private IAuthStore _authStore = null!;

    /// <summary>
    /// Preparación.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _authStore = new AuthStore();
        _ctx.Services.AddSingleton<IAuthStore>(_authStore);
    }

    /// <summary>
    /// Limpieza.
    /// </summary>
    [TearDown]
    public void TearDown() => _ctx.Dispose();

    /// <summary>
    /// Verifica que el componente muestre el título y el marcador de posición correctamente al iniciar sin token.
    /// </summary>
    [Test]
    public void Should_Render_Initial_State_Correctly()
    {
        // Act
        var cut = _ctx.Render<TokenDisplay>();

        // Assert
        cut.Find(".section-title").TextContent.Should().Be("Token JWT");
        cut.Find("#token").GetAttribute("placeholder").Should().Contain("Inicia sesión");
    }

    /// <summary>
    /// Valida que la UI se actualice automáticamente para mostrar el token cuando este se genera en el Store.
    /// </summary>
    [Test]
    public async Task Should_Display_Token_When_Set()
    {
        // Arrange
        var cut = _ctx.Render<TokenDisplay>();
        var testToken = "abc-123-def";

        // Act
        await _ctx.Renderer.Dispatcher.InvokeAsync(() => _authStore.SetToken(testToken));
        
        // Assert
        cut.WaitForAssertion(() => cut.Find("#token").GetAttribute("value").Should().Be(testToken));
    }

    /// <summary>
    /// Comprueba que el campo del token se limpie visualmente tras un Logout.
    /// </summary>
    [Test]
    public async Task Should_Clear_Display_On_Logout()
    {
        // Arrange
        await _ctx.Renderer.Dispatcher.InvokeAsync(() => _authStore.SetToken("initial-token"));
        var cut = _ctx.Render<TokenDisplay>();

        // Act
        await _ctx.Renderer.Dispatcher.InvokeAsync(() => _authStore.Logout());

        // Assert
        cut.WaitForState(() => string.IsNullOrEmpty(cut.Find("#token").GetAttribute("value")));
        cut.Find("#token").GetAttribute("value").Should().BeEmpty();
    }
}