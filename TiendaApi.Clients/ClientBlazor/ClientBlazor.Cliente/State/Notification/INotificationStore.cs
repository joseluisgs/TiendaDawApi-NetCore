using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace ClientBlazor.Cliente.State.Notifications;

/// <summary>
/// Define los tipos de notificaciones visuales soportadas por el sistema.
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Representa una notificación individual enviada al sistema.
/// </summary>
public record Notification
{
    public string Id { get; init; } = "";
    public NotificationType Type { get; init; } = NotificationType.Info;
    public string Message { get; init; } = "";
    public string? Title { get; init; } = null;
    public DateTime CreatedAt { get; init; } = default;
    public int DurationMs { get; init; } = 2000;
    public bool Dismissable { get; init; } = true;

    public bool IsAutoDismiss => DurationMs > 0 && Dismissable;
    public bool IsExpired => CreatedAt != default && DateTime.Now - CreatedAt > TimeSpan.FromMilliseconds(DurationMs);
}

/// <summary>
/// Representa el estado inmutable de la cola de notificaciones.
/// </summary>
public record NotificationState
{
    public List<Notification> Notifications { get; init; } = new();
    public Notification? Current { get; init; } = null;
    public bool IsLoading { get; init; } = false;
    public string? Error { get; init; } = null;

    public int Count => Notifications.Count;
    public bool HasNotifications => Notifications.Count > 0;
    public bool HasCurrent => Current != null;
}

/// <summary>
/// Define el contrato para el almacén de notificaciones globales.
/// </summary>
public interface INotificationStore
{
    IObservable<NotificationState> State { get; }
    IObservable<List<Notification>> Notifications { get; }
    IObservable<Notification?> Current { get; }
    
    NotificationState GetState();
    void Info(string message, string? title = null, int durationMs = 3000);
    void Success(string message, string? title = null, int durationMs = 3000);
    void Warning(string message, string? title = null, int durationMs = 3000);
    void Error(string message, string? title = null, int durationMs = 3000);
    void ErrorFromException(Exception ex, string? title = null, int durationMs = 0);
    void Dismiss(string id);
    void DismissCurrent();
    void Clear();
    void CleanupExpired();
    void SetCurrent(Notification? notification);
    Notification? GetAndClearCurrent();
    void SetLoading(bool isLoading);
    void SetError(string? error);
    IObservable<T> Select<T>(Func<NotificationState, T> selector);
}
