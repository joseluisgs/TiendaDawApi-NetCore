using ClientBlazor.Cliente.State.Auth;
using FluentAssertions;
using NUnit.Framework;

namespace ClientBlazor.Tests.State;

/// <summary>
/// Pruebas para el almacén de estado de autenticación (AuthStore).
/// Objetivo: Validar la gestión reactiva de la sesión del usuario.
/// </summary>
[TestFixture]
public class AuthStoreTests
{
    private AuthStore _authStore = null!;

    /// <summary>
    /// Inicializa una nueva instancia limpia de AuthStore antes de cada test.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _authStore = new AuthStore();
    }

    /// <summary>
    /// Comprueba que al arrancar, el estado inicial sea de no autenticado.
    /// </summary>
    [Test]
    public void Initial_State_Should_Be_Unauthenticated()
    {
        var state = _authStore.GetState();
        state.IsAuthenticated.Should().BeFalse();
        state.Token.Should().BeNull();
        state.Email.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que al establecer los datos de sesión, todos los campos del estado se actualicen.
    /// </summary>
    [Test]
    public void SetAuth_Should_Update_All_Fields_Correctly()
    {
        // Arrange
        var token = "test-token";
        var email = "test@example.com";
        var nombre = "Test User";
        var role = "ADMIN";

        // Act
        _authStore.SetAuth(token, email, nombre, role);

        // Assert
        var state = _authStore.GetState();
        state.IsAuthenticated.Should().BeTrue();
        state.Token.Should().Be(token);
        state.Email.Should().Be(email);
        state.Nombre.Should().Be(nombre);
        state.Role.Should().Be(role);
        state.IsAdmin.Should().BeTrue();
    }

    /// <summary>
    /// Valida que el proceso de Logout limpie todos los datos sensibles del estado.
    /// </summary>
    [Test]
    public void Logout_Should_Reset_State_To_Defaults()
    {
        // Arrange
        _authStore.SetAuth("token", "email", "name", "role");

        // Act
        _authStore.Logout();

        // Assert
        var state = _authStore.GetState();
        state.IsAuthenticated.Should().BeFalse();
        state.Token.Should().BeNull();
        state.Email.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica el comportamiento reactivo: los observadores deben recibir el nuevo token.
    /// </summary>
    [Test]
    public void TokenObservable_Should_Emit_New_Values()
    {
        // Arrange
        string? lastValue = null;
        using var subscription = _authStore.TokenObservable.Subscribe(val => lastValue = val);

        // Act
        _authStore.SetAuth("new-token", "e", "n", "r");

        // Assert
        lastValue.Should().Be("new-token");
    }

    /// <summary>
    /// Comprueba que la detección del rol de administrador no sea sensible a mayúsculas/minúsculas.
    /// </summary>
    [Test]
    public void IsAdmin_Should_Be_Case_Insensitive()
    {
        // Admin lowercase
        _authStore.SetAuth("token", "email", "name", "admin");
        _authStore.GetState().IsAdmin.Should().BeTrue();

        // Admin uppercase
        _authStore.SetAuth("token", "email", "name", "ADMIN");
        _authStore.GetState().IsAdmin.Should().BeTrue();
    }
}