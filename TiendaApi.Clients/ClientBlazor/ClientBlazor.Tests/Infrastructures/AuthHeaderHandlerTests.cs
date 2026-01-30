using ClientBlazor.Cliente.Infrastructures.Handlers;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using ClientBlazor.Cliente.Services.Storage;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Net;

namespace ClientBlazor.Tests.Infrastructures;

/// <summary>
/// Pruebas unitarias para el interceptor de mensajes HTTP (Handler).
/// Objetivo: Validar la seguridad transversal y la gestión automática de sesiones.
/// </summary>
[TestFixture]
public class AuthHeaderHandlerTests
{
    private Mock<IAuthStore> _authStoreMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;
    private Mock<ILocalStorageService> _storageMock = null!;
    private AuthHeaderHandler _handler = null!;

    /// <summary>
    /// Configura el entorno de aislamiento para probar el handler sin red real.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _authStoreMock = new Mock<IAuthStore>();
        _notificationStoreMock = new Mock<INotificationStore>();
        _storageMock = new Mock<ILocalStorageService>();
        _handler = new AuthHeaderHandler(_authStoreMock.Object, _notificationStoreMock.Object, _storageMock.Object);
    }

    /// <summary>
    /// Verifica que el interceptor añada correctamente el token JWT recuperado 
    /// del Store en la cabecera 'Authorization' de la petición saliente.
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Add_Authorization_Header_When_Token_Exists()
    {
        // Arrange
        _authStoreMock.Setup(s => s.GetState()).Returns(new AuthStore.AuthState { Token = "token123" });
        var innerHandler = new MockHttpMessageHandler(HttpStatusCode.OK);
        _handler.InnerHandler = innerHandler;
        var client = new HttpClient(_handler);

        // Act
        await client.GetAsync("http://test.com");

        // Assert
        innerHandler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("token123");
    }

    /// <summary>
    /// Valida el sistema de detección de sesiones expiradas: ante un error 401 del servidor, 
    /// el handler debe limpiar automáticamente el estado local y notificar al usuario.
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Handle_401_By_Logging_Out()
    {
        // Arrange - El usuario cree estar logueado pero el servidor dice 401
        _authStoreMock.Setup(s => s.GetState()).Returns(new AuthStore.AuthState { Token = "expirado", Role = "USER" });
        var innerHandler = new MockHttpMessageHandler(HttpStatusCode.Unauthorized);
        _handler.InnerHandler = innerHandler;
        var client = new HttpClient(_handler);

        // Act
        await client.GetAsync("http://test.com");

        // Assert
        _authStoreMock.Verify(s => s.Logout(), Times.Once);
        _storageMock.Verify(s => s.RemoveItemAsync("auth_session"), Times.Once);
        _notificationStoreMock.Verify(n => n.Error(It.IsAny<string>(), "Sesión Expirada", It.IsAny<int>()), Times.Once);
    }

    /// <summary>
    /// Handler auxiliar para interceptar y capturar las peticiones en el entorno de test.
    /// </summary>
    private class MockHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}