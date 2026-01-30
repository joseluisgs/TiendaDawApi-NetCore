using Bunit;
using ClientBlazor.Cliente.Components.Shared;
using ClientBlazor.Cliente.State.Auth;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Moq;

namespace ClientBlazor.Tests.Components;

/// <summary>
/// Pruebas para el menú de navegación.
/// Objetivo: Validar la consistencia de las rutas y el dinamismo del menú según la sesión.
/// </summary>
[TestFixture]
public class NavMenuTests
{
    private BunitContext _ctx = null!;
    private Mock<IAuthStore> _authStoreMock = null!;

    /// <summary>
    /// Configura el mock del estado de sesión.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _authStoreMock = new Mock<IAuthStore>();
        
        // Estado inicial: no autenticado
        _authStoreMock.Setup(s => s.IsAuthenticatedObservable).Returns(System.Reactive.Linq.Observable.Return(false));
        _authStoreMock.Setup(s => s.IsAdminObservable).Returns(System.Reactive.Linq.Observable.Return(false));

        _ctx.Services.AddSingleton(_authStoreMock.Object);
    }

    /// <summary>
    /// Limpieza.
    /// </summary>
    [TearDown]
    public void TearDown() => _ctx.Dispose();

    /// <summary>
    /// Verifica que los enlaces principales (REST y GraphQL) siempre estén disponibles en el menú.
    /// </summary>
    [Test]
    public void NavMenu_Should_Always_Render_Basic_Links()
    {
        // Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Find("a[href='rest']").Should().NotBeNull();
        cut.Find("a[href='/graphql']").Should().NotBeNull();
    }

    /// <summary>
    /// Comprueba que el enlace a la página de inicio esté presente.
    /// </summary>
    [Test]
    public void NavMenu_Should_Render_Home_Link()
    {
        // Act
        var cut = _ctx.Render<NavMenu>();

        // Assert
        cut.Find("a[href='']").Should().NotBeNull();
    }
}