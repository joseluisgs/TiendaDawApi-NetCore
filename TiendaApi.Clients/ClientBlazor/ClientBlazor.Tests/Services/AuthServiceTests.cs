using ClientBlazor.Cliente.Clients;
using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.DTOs.Auth;
using ClientBlazor.Cliente.Services.Rest;
using ClientBlazor.Cliente.Services.Storage;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Refit;
using System.Net;

namespace ClientBlazor.Tests.Services;

/// <summary>
/// Pruebas unitarias para el servicio de orquestación de autenticación.
/// Objetivo: Validar la lógica de negocio de inicio de sesión, mapeo de errores y persistencia.
/// </summary>
[TestFixture]
public class AuthServiceTests
{
    private Mock<ITiendaRestClient> _clientMock = null!;
    private Mock<IAuthStore> _authStoreMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;
    private Mock<ILocalStorageService> _storageMock = null!;
    private AuthService _authService = null!;

    /// <summary>
    /// Prepara el servicio con dependencias simuladas.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _clientMock = new Mock<ITiendaRestClient>();
        _authStoreMock = new Mock<IAuthStore>();
        _notificationStoreMock = new Mock<INotificationStore>();
        _storageMock = new Mock<ILocalStorageService>();
        _authService = new AuthService(_clientMock.Object, _authStoreMock.Object, _notificationStoreMock.Object, _storageMock.Object);
    }

    /// <summary>
    /// Comprueba que la validación local bloquee el envío de datos vacíos a la red.
    /// </summary>
    [Test]
    public async Task LoginAsync_Should_Return_Error_When_Email_Is_Empty()
    {
        // Act
        var result = await _authService.LoginAsync("", "password");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ValidationErrors.EmptyField("email").Code);
        _clientMock.Verify(c => c.LoginAsync(It.IsAny<LoginDto>()), Times.Never);
    }

    /// <summary>
    /// Valida que un inicio de sesión exitoso actualice el estado global, 
    /// persista los datos en disco y notifique al usuario.
    /// </summary>
    [Test]
    public async Task LoginAsync_Should_Update_Store_On_Success()
    {
        // Arrange
        var userDto = new UserDto { Id = 1, Username = "testuser", Email = "test@test.com", Role = "USER" };
        var authResponse = new AuthResponseDto { Token = "token123", User = userDto };
        _clientMock.Setup(c => c.LoginAsync(It.IsAny<LoginDto>())).ReturnsAsync(authResponse);

        // Act
        var result = await _authService.LoginAsync("test@test.com", "password123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("token123");
        
        _authStoreMock.Verify(s => s.SetAuth("token123", "test@test.com", "testuser", "USER"), Times.Once);
        _storageMock.Verify(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<AuthStore.AuthState>()), Times.Once);
        _notificationStoreMock.Verify(n => n.Success(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    /// <summary>
    /// Verifica que el servicio traduzca correctamente los fallos de red 
    /// (como un 401 Unauthorized) en errores legibles del dominio cliente.
    /// </summary>
    [Test]
    public async Task LoginAsync_Should_Return_DomainError_On_ApiException()
    {
        // Arrange
        var apiException = await ApiException.Create(
            new HttpRequestMessage(), 
            HttpMethod.Post, 
            new HttpResponseMessage(HttpStatusCode.Unauthorized), 
            new RefitSettings());
            
        _clientMock.Setup(c => c.LoginAsync(It.IsAny<LoginDto>())).ThrowsAsync(apiException);

        // Act
        var result = await _authService.LoginAsync("test@test.com", "wrong");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AuthErrors.InvalidCredentials.Code);
        _notificationStoreMock.Verify(n => n.Error(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }
}