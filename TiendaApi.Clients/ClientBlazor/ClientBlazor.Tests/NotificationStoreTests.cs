using ClientBlazor.Cliente.State.Notifications;
using FluentAssertions;
using NUnit.Framework;

namespace ClientBlazor.Tests.State;

/// <summary>
/// Pruebas para el almacén de notificaciones globales.
/// Objetivo: Validar la gestión de colas de mensajes y la detección de duplicados.
/// </summary>
[TestFixture]
public class NotificationStoreTests
{
    private NotificationStore _store = null!;

    /// <summary>
    /// Prepara el entorno para cada prueba.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _store = new NotificationStore();
    }

    /// <summary>
    /// Verifica que al iniciar el almacén no haya mensajes pendientes.
    /// </summary>
    [Test]
    public void Initial_State_Should_Have_No_Notifications()
    {
        _store.GetState().Notifications.Should().BeEmpty();
        _store.GetState().Current.Should().BeNull();
    }

    /// <summary>
    /// Comprueba que al añadir una notificación exitosa, esta aparezca en la lista y sea marcada como actual.
    /// </summary>
    [Test]
    public void Success_Should_Add_Notification_And_Set_As_Current()
    {
        // Act
        _store.Success("Operacion exitosa", "Exito");

        // Assert
        var state = _store.GetState();
        state.Count.Should().Be(1);
        state.Current.Should().NotBeNull();
        state.Current!.Message.Should().Be("Operacion exitosa");
        state.Current.Type.Should().Be(NotificationType.Success);
    }

    /// <summary>
    /// Valida que el almacén limite el crecimiento de la lista a un máximo de 3 elementos 
    /// para no saturar visualmente la interfaz de usuario.
    /// </summary>
    [Test]
    public void Store_Should_Maintain_Maximum_Of_3_Notifications()
    {
        // Act
        _store.Info("1");
        _store.Info("2");
        _store.Info("3");
        _store.Info("4");

        // Assert
        _store.GetState().Count.Should().Be(3);
        _store.GetState().Notifications.First().Message.Should().Be("2");
    }

    /// <summary>
    /// Verifica que se pueda eliminar una notificación específica mediante su ID único.
    /// </summary>
    [Test]
    public void Dismiss_Should_Remove_Notification_By_Id()
    {
        // Arrange
        _store.Info("Test");
        var id = _store.GetState().Current!.Id;

        // Act
        _store.Dismiss(id);

        // Assert
        _store.GetState().Notifications.Should().BeEmpty();
    }

    /// <summary>
    /// Valida la lógica de duplicados: si llega el mismo mensaje, no se añade una nueva entrada 
    /// pero se actualiza la fecha para reiniciar el temporizador visual.
    /// </summary>
    [Test]
    public async Task Duplicate_Messages_Should_Not_Add_New_Notification_But_Update_Timestamp()
    {
        // Arrange
        _store.Success("Mensaje", "Titulo");
        var initialTime = _store.GetState().Current!.CreatedAt;
        
        // Esperar un poco para asegurar que el reloj avance
        await Task.Delay(50);

        // Act
        _store.Success("Mensaje", "Titulo");

        // Assert
        _store.GetState().Count.Should().Be(1);
        _store.GetState().Current!.CreatedAt.Should().BeAfter(initialTime);
    }

    /// <summary>
    /// Comprueba que la limpieza total del almacén funcione correctamente.
    /// </summary>
    [Test]
    public void Clear_Should_Remove_All_Notifications()
    {
        // Arrange
        _store.Info("1");
        _store.Info("2");

        // Act
        _store.Clear();

        // Assert
        _store.GetState().Notifications.Should().BeEmpty();
    }
}