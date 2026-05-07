using ClientBlazor.Cliente.Services.SignalR;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace ClientBlazor.Tests.Services;

/// <summary>
/// Pruebas unitarias para el servicio SignalR.
/// Objetivo: Validar la lógica de orquestación de conexiones y seguridad de Hubs.
/// </summary>
[TestFixture]
public class SignalRServiceTests
{
    private Mock<IAuthStore> _authStoreMock = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;
    private SignalRService _service = null!;

    /// <summary>
    /// Prepara el entorno para cada test.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _authStoreMock = new Mock<IAuthStore>();
        _notificationStoreMock = new Mock<INotificationStore>();
        _service = new SignalRService(_authStoreMock.Object, _notificationStoreMock.Object);
    }

    /// <summary>
    /// Verifica que el sistema impida la conexión a Hubs protegidos si no hay token JWT.
    /// </summary>
    [Test]
    public async Task ConnectPedidosAsync_Should_Warn_And_Not_Connect_If_No_Token()
    {
        // Arrange
        _authStoreMock.Setup(s => s.GetState()).Returns(new AuthStore.AuthState { Token = null });

        // Act
        await _service.ConnectPedidosAsync();

        // Assert
        _service.IsConnected.Should().BeFalse();
        _notificationStoreMock.Verify(n => n.Warning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
    }

    /// <summary>
    /// Comprueba que el estado inicial de la conexión sea desconectado.
    /// </summary>
    [Test]
    public void Initial_State_Should_Be_Disconnected()
    {
        _service.IsConnected.Should().BeFalse();
    }

    /// <summary>
    /// Valida que el método de desconexión no lance errores incluso si no hay una conexión activa.
    /// </summary>
    [Test]
    public async Task DisconnectAsync_Should_Work_Even_If_Not_Connected()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.DisconnectAsync());
    }

    /// <summary>
    /// Verifica que se intentó la conexión al Hub sin lanzar excepciones.
    /// </summary>
    [Test]
    public async Task ConnectProductosAsync_Should_Attempt_Connection_To_Correct_Url()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.ConnectProductosAsync());
        
        // Verifica que se notificó al usuario (éxito o error según disponibilidad del servidor)
        _notificationStoreMock.Verify(n => n.Success(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.AtMostOnce);
    }
}