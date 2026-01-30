using Bunit;
using ClientBlazor.Cliente.Components.Shared;
using ClientBlazor.Cliente.State.Notifications;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System.Reactive.Subjects;

namespace ClientBlazor.Tests.Components;

/// <summary>
/// Pruebas para el panel de notificaciones (Toasts).
/// Objetivo: Validar la reactividad de la UI ante eventos del stream de notificaciones globales.
/// </summary>
[TestFixture]
public class ToastPanelTests
{
    private BunitContext _ctx = null!;
    private Mock<INotificationStore> _notificationStoreMock = null!;
    private BehaviorSubject<List<Notification>> _notificationsSubject = null!;

    /// <summary>
    /// Configura el mock del stream de notificaciones.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _ctx = new BunitContext();
        _notificationStoreMock = new Mock<INotificationStore>();
        
        // Creamos un Subject para controlar las emisiones de notificaciones
        _notificationsSubject = new BehaviorSubject<List<Notification>>(new List<Notification>());
        _notificationStoreMock.Setup(s => s.Notifications).Returns(_notificationsSubject);

        _ctx.Services.AddSingleton(_notificationStoreMock.Object);
    }

    /// <summary>
    /// Limpieza.
    /// </summary>
    [TearDown]
    public void TearDown() => _ctx.Dispose();

    /// <summary>
    /// Verifica que no se renderice ningún Toast si la lista de notificaciones está vacía.
    /// </summary>
    [Test]
    public void Should_Not_Render_Toasts_When_List_Is_Empty()
    {
        var cut = _ctx.Render<ToastPanel>();
        cut.FindAll(".toast-item").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que al emitir una notificación desde el Store, esta aparezca con sus datos y estilos correctos en el DOM.
    /// </summary>
    [Test]
    public void Should_Render_Toast_When_Notification_Is_Added()
    {
        var cut = _ctx.Render<ToastPanel>();
        var notification = new Notification
        {
            Id = "1",
            Type = NotificationType.Success,
            Message = "Test Message",
            Title = "Test Title",
            CreatedAt = DateTime.Now
        };

        // Act - Simulamos la emisión reactiva del Store
        _ctx.Renderer.Dispatcher.InvokeAsync(() => _notificationsSubject.OnNext(new List<Notification> { notification }));

        // Assert
        cut.WaitForState(() => cut.FindAll(".toast-item").Count > 0);
        cut.Find(".toast-title").TextContent.Should().Be("Test Title");
        cut.Find(".toast-message").TextContent.Should().Be("Test Message");
        cut.Find(".toast-item").ClassList.Should().Contain("toast-success");
    }

    /// <summary>
    /// Comprueba que al hacer clic sobre una notificación se llame al método Dismiss del Store.
    /// </summary>
    [Test]
    public void Clicking_Toast_Should_Call_Dismiss()
    {
        // Arrange
        var notification = new Notification { Id = "99", Message = "Click me", CreatedAt = DateTime.Now };
        _notificationsSubject.OnNext(new List<Notification> { notification });
        var cut = _ctx.Render<ToastPanel>();

        // Act
        cut.Find(".toast-item").Click();

        // Assert
        _notificationStoreMock.Verify(s => s.Dismiss("99"), Times.Once);
    }
}
