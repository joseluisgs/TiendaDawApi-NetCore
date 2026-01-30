using NUnit.Framework;
using FluentAssertions;
using ClientBlazor.Cliente.State;
using ClientBlazor.Cliente.DTOs.Auth;
using System.Reactive.Linq;

namespace ClientBlazor.Tests.State;

[TestFixture]
public class AuthStoreTests
{
    private AuthStore _authStore = null!;

    [SetUp]
    public void SetUp()
    {
        // Reiniciamos el store antes de cada test
        _authStore = new AuthStore();
    }

    [Test]
    public void Initial_State_Should_Be_Not_Authenticated()
    {
        // Act
        var state = _authStore.GetState();

        // Assert - Usando FluentAssertions
        state.IsAuthenticated.Should().BeFalse();
        state.Token.Should().BeNullOrEmpty();
        // El estado inicial tiene strings vacíos, no nulos, según la definición del record
        state.Email.Should().BeEmpty();
        state.Nombre.Should().BeEmpty();
        state.Role.Should().BeEmpty();
        state.IsLoading.Should().BeFalse();
    }

    [Test]
    public void SetAuth_Should_Update_State_Correctly()
    {
        // Arrange
        var token = "test-jwt-token";
        var email = "test@example.com";
        var nombre = "Test User";
        var role = "USER";

        // Act
        _authStore.SetAuth(token, email, nombre, role);
        var state = _authStore.GetState();

        // Assert
        state.IsAuthenticated.Should().BeTrue();
        state.Token.Should().Be(token);
        state.Email.Should().Be(email);
        state.Nombre.Should().Be(nombre);
        state.Role.Should().Be(role);
    }

    [Test]
    public void Logout_Should_Clear_State()
    {
        // Arrange
        _authStore.SetAuth("token", "email", "name", "role");
        
        // Act
        _authStore.Logout();
        var state = _authStore.GetState();

        // Assert
        state.IsAuthenticated.Should().BeFalse();
        state.Token.Should().BeNull();
        state.Email.Should().BeEmpty();
    }

    [Test]
    public void IsAuthenticated_Observable_Should_Notify_Changes()
    {
        // Arrange
        bool? lastValue = null;
        using var subscription = _authStore.IsAuthenticatedObservable.Subscribe(val => lastValue = val);

        // Act 1: Initial state
        lastValue.Should().BeFalse();

        // Act 2: Login
        _authStore.SetAuth("token", "email", "name", "role");
        lastValue.Should().BeTrue();

        // Act 3: Logout
        _authStore.Logout();
        lastValue.Should().BeFalse();
    }

    [Test]
    public void IsAdmin_Should_Return_True_Only_For_Admin_Role()
    {
        // Act & Assert 1: User
        _authStore.SetAuth("token", "email", "name", "USER");
        _authStore.GetState().IsAdmin.Should().BeFalse();

        // Act & Assert 2: Admin
        _authStore.SetAuth("token", "email", "name", "ADMIN");
        _authStore.GetState().IsAdmin.Should().BeTrue();
        
        // Act & Assert 3: Case insensitive
        _authStore.SetAuth("token", "email", "name", "admin");
        _authStore.GetState().IsAdmin.Should().BeTrue();
    }
}