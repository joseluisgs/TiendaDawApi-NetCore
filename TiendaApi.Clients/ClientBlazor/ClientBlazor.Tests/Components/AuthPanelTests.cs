using Bunit;
using ClientBlazor.Cliente.Components.Shared;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.Services.Rest;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Moq;

namespace ClientBlazor.Tests.Components;

/// <summary>
/// Pruebas de UI para el panel de información del usuario autenticado.
/// Objetivo: Validar el renderizado condicional según el estado de la sesión.
/// </summary>
[TestFixture]
public class AuthPanelTests
{
    private BunitContext _ctx = null!;
    private IAuthStore _authStore = null!;
    private Mock<IAuthService> _authServiceMock = null!;

    /// <summary>
    /// Configura el entorno de renderizado.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _authStore = new AuthStore();
        _authServiceMock = new Mock<IAuthService>();
        
        _ctx.Services.AddSingleton<IAuthStore>(_authStore);
        _ctx.Services.AddSingleton<IAuthService>(_authServiceMock.Object);
    }

    /// <summary>
    /// Limpieza.
    /// </summary>
    [TearDown]
    public void TearDown() => _ctx.Dispose();

    /// <summary>
    /// Comprueba que el panel no sea visible si el usuario no ha iniciado sesión.
    /// </summary>
    [Test]
    public void Should_Not_Render_Anything_When_Not_Authenticated()
    {
        // Act
        var cut = _ctx.Render<AuthPanel>();

        // Assert
        cut.FindAll(".auth-panel").Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que la información del perfil del usuario se muestre correctamente al estar autenticado.
    /// </summary>
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

    /// <summary>
    /// Valida que se aplique el estilo visual de administrador cuando el rol es 'ADMIN'.
    /// </summary>
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

    /// <summary>
    /// Verifica que al pulsar el botón de cierre de sesión se invoque al servicio de autenticación.
    /// </summary>
    [Test]
    public void Clicking_Logout_Should_Call_AuthService_Logout()
    {
        // Arrange
        _authStore.SetAuth("token", "user", "User", "USER");
        var cut = _ctx.Render<AuthPanel>();

        // Act
        cut.Find("button.auth-logout-btn").Click();

        // Assert
        _authServiceMock.Verify(s => s.LogoutAsync(), Times.Once);
    }
}