using Bunit;
using ClientBlazor.Cliente.Components.Shared;
using ClientBlazor.Cliente.Services.Rest;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using ClientBlazor.Cliente.DTOs.Auth;
using ClientBlazor.Cliente.Domain.Errors;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using CSharpFunctionalExtensions;

namespace ClientBlazor.Tests.Components;

/// <summary>
/// Pruebas de UI para el componente de inicio de sesión.
/// Objetivo: Validar la interacción del usuario con el formulario y la orquestación con el servicio de autenticación.
/// </summary>
[TestFixture]
public class LoginTests
{
    private BunitContext _ctx = null!;
    private Mock<IAuthService> _authServiceMock = null!;
    private Mock<IAuthStore> _authStoreMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;

    /// <summary>
    /// Configura el contexto de renderizado de BUnit y registra los servicios mockeados.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _authServiceMock = new Mock<IAuthService>();
        _authStoreMock = new Mock<IAuthStore>();
        _notificationStoreMock = new Mock<INotificationStore>();

        _ctx.Services.AddSingleton(_authServiceMock.Object);
        _ctx.Services.AddSingleton(_authStoreMock.Object);
        _ctx.Services.AddSingleton(_notificationStoreMock.Object);
    }

    /// <summary>
    /// Limpia el contexto de BUnit tras cada test.
    /// </summary>
    [TearDown]
    public void TearDown() => _ctx.Dispose();

    /// <summary>
    /// Verifica que el formulario de login se renderice con todos sus controles básicos.
    /// </summary>
    [Test]
    public void Login_Component_Should_Render_Correctly()
    {
        // Act
        var cut = _ctx.Render<Login>();

        // Assert
        cut.Find(".login-title").TextContent.Should().Be("Iniciar Sesion");
        cut.Find("input[type='email']").Should().NotBeNull();
        cut.Find("input[type='password']").Should().NotBeNull();
        cut.Find("button.btn-login").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que al introducir credenciales y pulsar el botón, se llame al método Login del servicio con los datos correctos.
    /// </summary>
    [Test]
    public void Clicking_Login_Should_Call_AuthService_With_Input_Values()
    {
        // Arrange
        var cut = _ctx.Render<Login>();
        var email = "profe@daw.com";
        var pass = "password123";
        
        _authServiceMock.Setup(s => s.LoginAsync(email, pass))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(new AuthResponseDto { Token = "token", User = null! }));

        // Act
        cut.Find("input[type='email']").Change(email);
        cut.Find("input[type='password']").Change(pass);
        cut.Find("button.btn-login").Click();

        // Assert
        _authServiceMock.Verify(s => s.LoginAsync(email, pass), Times.Once);
    }

    /// <summary>
    /// Comprueba que los botones de 'Demo' rellenen automáticamente los campos del formulario.
    /// </summary>
    [Test]
    public void FillDemo_Buttons_Should_Populate_Fields()
    {
        // Act
        var cut = _ctx.Render<Login>();
        cut.FindAll("button.demo-user").First().Click(); // Boton Admin

        // Assert
        cut.Find("input[type='email']").GetAttribute("value").Should().NotBeEmpty();
        cut.Find("input[type='password']").GetAttribute("value").Should().NotBeEmpty();
    }
}