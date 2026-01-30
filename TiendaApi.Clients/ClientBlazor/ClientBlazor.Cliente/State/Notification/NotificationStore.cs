using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace ClientBlazor.Cliente.State.Notifications;

/// <inheritdoc cref="INotificationStore" />
public class NotificationStore : INotificationStore
{
    private readonly BehaviorSubject<NotificationState> _state;
    private int _counter = 0;
    
    /// <inheritdoc />
    public IObservable<NotificationState> State => _state.AsObservable();
    /// <inheritdoc />
    public IObservable<List<Notification>> Notifications => _state.Select(s => s.Notifications).DistinctUntilChanged();
    /// <inheritdoc />
    public IObservable<Notification?> Current => _state.Select(s => s.Current).DistinctUntilChanged();

    public NotificationStore()
    {
        _state = new BehaviorSubject<NotificationState>(new NotificationState());
    }

    /// <inheritdoc />
    public NotificationState GetState() => _state.Value;

    /// <inheritdoc />
    public void Info(string message, string? title = null, int durationMs = 3000) => AddNotification(NotificationType.Info, message, title, durationMs);
    /// <inheritdoc />
    public void Success(string message, string? title = null, int durationMs = 3000) => AddNotification(NotificationType.Success, message, title, durationMs);
    /// <inheritdoc />
    public void Warning(string message, string? title = null, int durationMs = 3000) => AddNotification(NotificationType.Warning, message, title, durationMs);
    /// <inheritdoc />
    public void Error(string message, string? title = null, int durationMs = 3000) => AddNotification(NotificationType.Error, message, title, durationMs);

    /// <inheritdoc />
    public void ErrorFromException(Exception ex, string? title = null, int durationMs = 0)
    {
        var message = ex.InnerException != null ? $"{ex.Message}: {ex.InnerException.Message}" : ex.Message;
        AddNotification(NotificationType.Error, message, title ?? "Error", durationMs);
    }

    private void AddNotification(NotificationType type, string message, string? title, int durationMs)
    {
        var currentList = _state.Value.Notifications.ToList();
        var defaultTitle = title ?? GetDefaultTitle(type);
        var duplicate = currentList.FirstOrDefault(n => n.Type == type && n.Message == message && n.Title == defaultTitle);

        if (duplicate != null)
        {
            var updatedNotification = duplicate with { CreatedAt = DateTime.Now };
            var updatedList = currentList.Select(n => n.Id == duplicate.Id ? updatedNotification : n).ToList();
            _state.OnNext(_state.Value with { Notifications = updatedList, Current = updatedNotification, Error = null });
            return;
        }

        var notification = new Notification 
        { 
            Id = $"{++_counter}", 
            Type = type, 
            Message = message, 
            Title = defaultTitle, 
            CreatedAt = DateTime.Now, 
            DurationMs = durationMs, 
            Dismissable = durationMs != -1 
        };
        var newList = currentList.Concat(new[] { notification }).TakeLast(3).ToList();
        _state.OnNext(_state.Value with { Notifications = newList, Current = notification, Error = null });
    }

    /// <inheritdoc />
    public void Dismiss(string id)
    {
        var newList = _state.Value.Notifications.Where(n => n.Id != id).ToList();
        _state.OnNext(_state.Value with { Notifications = newList, Current = _state.Value.Current?.Id == id ? null : _state.Value.Current });
    }

    /// <inheritdoc />
    public void DismissCurrent()
    {
        var newList = _state.Value.Notifications.Where(n => n.Id != _state.Value.Current?.Id).ToList();
        _state.OnNext(_state.Value with { Notifications = newList, Current = newList.Count > 0 ? newList.Last() : null });
    }

    /// <inheritdoc />
    public void Clear() => _state.OnNext(new NotificationState());

    /// <inheritdoc />
    public void CleanupExpired()
    {
        var newList = _state.Value.Notifications.Where(n => !n.IsExpired).ToList();
        var current = _state.Value.Current != null && !_state.Value.Current.IsExpired ? _state.Value.Current : null;
        _state.OnNext(_state.Value with { Notifications = newList, Current = current });
    }

    /// <inheritdoc />
    public void SetCurrent(Notification? notification) => _state.OnNext(_state.Value with { Current = notification });

    /// <inheritdoc />
    public Notification? GetAndClearCurrent()
    {
        var current = _state.Value.Current;
        if (current != null) Dismiss(current.Id);
        return current;
    }

    /// <inheritdoc />
    public void SetLoading(bool isLoading) => _state.OnNext(_state.Value with { IsLoading = isLoading });
    /// <inheritdoc />
    public void SetError(string? error) => _state.OnNext(_state.Value with { Error = error });

    private static string GetDefaultTitle(NotificationType type) => type switch {
        NotificationType.Info => "Información",
        NotificationType.Success => "¡Éxito!",
        NotificationType.Warning => "Advertencia",
        NotificationType.Error => "Error",
        _ => "Notificación"
    };

    /// <inheritdoc />
    public IObservable<T> Select<T>(Func<NotificationState, T> selector) => _state.Select(selector).DistinctUntilChanged();
}