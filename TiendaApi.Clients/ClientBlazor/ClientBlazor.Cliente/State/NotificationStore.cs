using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace ClientBlazor.Cliente.State;

/// <summary>
/// Store de notificaciones con patrón Pinia-like usando RX (BehaviorSubject).
/// Maneja mensajes de éxito, error, warning e información para mostrar al usuario.
/// </summary>
public class NotificationStore
{
    /// <summary>
    /// Tipo de notificación.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>Información general.</summary>
        Info,
        /// <summary>Operación exitosa.</summary>
        Success,
        /// <summary>Advertencia.</summary>
        Warning,
        /// <summary>Error.</summary>
        Error
    }

    /// <summary>
    /// Representa una notificación individual.
    /// </summary>
    public record Notification(
        string Id = "",
        NotificationType Type = NotificationType.Info,
        string Message = "",
        string? Title = null,
        DateTime CreatedAt = default,
        int DurationMs = 5000,
        bool Dismissable = true
    )
    {
        public bool IsAutoDismiss => DurationMs > 0 && Dismissable;
        public bool IsExpired => CreatedAt != default && DateTime.Now - CreatedAt > TimeSpan.FromMilliseconds(DurationMs);
    }

    /// <summary>
    /// Estado inmutable del store de notificaciones.
    /// </summary>
    public record NotificationState(
        List<Notification> Notifications = default!,
        Notification? Current = null,
        bool IsLoading = false,
        string? Error = null
    )
    {
        public int Count => Notifications.Count;
        public bool HasNotifications => Notifications.Count > 0;
        public bool HasCurrent => Current != null;
    }

    private readonly BehaviorSubject<NotificationState> _state;
    private int _counter = 0;
    
    /// <summary>
    /// Observable del estado completo.
    /// </summary>
    public IObservable<NotificationState> State => _state.AsObservable();

    /// <summary>
    /// Getter reactivo: lista de notificaciones activas.
    /// </summary>
    public IObservable<List<Notification>> Notifications => _state.Select(s => s.Notifications).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: notificación actual (para toasts principales).
    /// </summary>
    public IObservable<Notification?> Current => _state.Select(s => s.Current).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: indica si hay notificaciones pendientes.
    /// </summary>
    public IObservable<bool> HasNotifications => _state.Select(s => s.HasNotifications).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: número de notificaciones pendientes.
    /// </summary>
    public IObservable<int> Count => _state.Select(s => s.Count).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: indica si hay una notificación actual.
    /// </summary>
    public IObservable<bool> HasCurrent => _state.Select(s => s.HasCurrent).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: stream de nuevas notificaciones (para efectos).
    /// </summary>
    public IObservable<Notification> OnNotificationAdded => _state.SelectMany(_ => 
        _state.Value.HasNotifications ? new[] { _state.Value.Notifications.Last() }.ToObservable() : Observable.Empty<Notification>());

    /// <summary>
    /// Inicializa el store con estado inicial vacío.
    /// </summary>
    public NotificationStore()
    {
        _state = new BehaviorSubject<NotificationState>(new NotificationState(Notifications: new List<Notification>()));
    }

    /// <summary>
    /// Obtiene el estado actual.
    /// </summary>
    /// <returns>Estado actual de notificaciones.</returns>
    public NotificationState GetState() => _state.Value;

    /// <summary>
    /// Action: Muestra una notificación de información.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="title">Título opcional.</param>
    /// <param name="durationMs">Duración en milisegundos (0 = manual).</param>
    public void Info(string message, string? title = null, int durationMs = 3000)
    {
        AddNotification(NotificationType.Info, message, title, durationMs);
    }

    /// <summary>
    /// Action: Muestra una notificación de éxito.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="title">Título opcional.</param>
    /// <param name="durationMs">Duración en milisegundos (0 = manual).</param>
    public void Success(string message, string? title = null, int durationMs = 3000)
    {
        AddNotification(NotificationType.Success, message, title, durationMs);
    }

    /// <summary>
    /// Action: Muestra una notificación de advertencia.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="title">Título opcional.</param>
    /// <param name="durationMs">Duración en milisegundos (0 = manual).</param>
    public void Warning(string message, string? title = null, int durationMs = 3000)
    {
        AddNotification(NotificationType.Warning, message, title, durationMs);
    }

    /// <summary>
    /// Action: Muestra una notificación de error.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    /// <param name="title">Título opcional.</param>
    /// <param name="durationMs">Duración en milisegundos (por defecto 3 segundos).</param>
    public void Error(string message, string? title = null, int durationMs = 3000)
    {
        AddNotification(NotificationType.Error, message, title, durationMs);
    }

    /// <summary>
    /// Action: Muestra una notificación desde una excepción.
    /// </summary>
    /// <param name="ex">Excepción a mostrar.</param>
    /// <param name="title">Título opcional.</param>
    /// <param name="durationMs">Duración en milisegundos.</param>
    public void ErrorFromException(Exception ex, string? title = null, int durationMs = 0)
    {
        var message = ex.Message;
        if (ex.InnerException != null)
        {
            message = $"{ex.Message}: {ex.InnerException.Message}";
        }
        AddNotification(NotificationType.Error, message, title ?? "Error", durationMs);
    }

    /// <summary>
    /// Action: Añade una notificación al sistema.
    /// Evita duplicados del mismo tipo con el mismo mensaje.
    /// </summary>
    /// <param name="type">Tipo de notificación.</param>
    /// <param name="message">Mensaje.</param>
    /// <param name="title">Título opcional.</param>
    /// <param name="durationMs">Duración.</param>
    private void AddNotification(NotificationType type, string message, string? title, int durationMs)
    {
        var currentList = _state.Value.Notifications.ToList();

        // Evitar duplicados: si ya existe una notificación del mismo tipo con el mismo mensaje, no añadir
        var duplicate = currentList.FirstOrDefault(n =>
            n.Type == type && n.Message == message && n.Title == (title ?? GetDefaultTitle(type)));

        if (duplicate != null)
        {
            // Si es un duplicado, resetear su tiempo de creación para que dure más
            var updatedList = currentList.Select(n =>
                n.Id == duplicate.Id
                    ? n with { CreatedAt = DateTime.Now }
                    : n).ToList();

            var duplicateState = _state.Value with
            {
                Notifications = updatedList,
                Current = duplicate,
                Error = null
            };
            _state.OnNext(duplicateState);
            return;
        }

        var notification = new Notification(
            Id: $"{++_counter}",
            Type: type,
            Message: message,
            Title: title ?? GetDefaultTitle(type),
            CreatedAt: DateTime.Now,
            DurationMs: durationMs,
            Dismissable: durationMs != -1
        );

        var newList = currentList.Concat(new[] { notification }).ToList();

        // Limitar a 3 notificaciones máximo
        if (newList.Count > 3)
        {
            newList = newList.TakeLast(3).ToList();
        }

        var newState = _state.Value with
        {
            Notifications = newList,
            Current = notification,
            Error = null
        };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Elimina una notificación por ID.
    /// </summary>
    /// <param name="id">ID de la notificación.</param>
    public void Dismiss(string id)
    {
        var newList = _state.Value.Notifications.Where(n => n.Id != id).ToList();
        var current = _state.Value.Current?.Id == id ? null : _state.Value.Current;

        var newState = _state.Value with
        {
            Notifications = newList,
            Current = current
        };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Elimina la notificación actual.
    /// </summary>
    public void DismissCurrent()
    {
        var newList = _state.Value.Notifications
            .Where(n => n.Id != _state.Value.Current?.Id)
            .ToList();

        var newState = _state.Value with
        {
            Notifications = newList,
            Current = newList.Count > 0 ? newList.Last() : null
        };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Elimina todas las notificaciones.
    /// Similar a Pinia: store.$reset()
    /// </summary>
    public void Clear()
    {
        _state.OnNext(new NotificationState(Notifications: new List<Notification>()));
    }

    /// <summary>
    /// Action: Elimina las notificaciones expiradas.
    /// </summary>
    public void CleanupExpired()
    {
        var newList = _state.Value.Notifications
            .Where(n => !n.IsExpired)
            .ToList();

        var current = _state.Value.Current != null && !_state.Value.Current.IsExpired 
            ? _state.Value.Current 
            : null;

        var newState = _state.Value with
        {
            Notifications = newList,
            Current = current
        };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Establece la notificación actual manualmente.
    /// </summary>
    /// <param name="notification">Notificación a mostrar.</param>
    public void SetCurrent(Notification? notification)
    {
        var newState = _state.Value with { Current = notification };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Obtiene y elimina la notificación actual ( patrón queue).
    /// </summary>
    /// <returns>La notificación actual o null.</returns>
    public Notification? GetAndClearCurrent()
    {
        var current = _state.Value.Current;
        if (current != null)
        {
            Dismiss(current.Id);
        }
        return current;
    }

    /// <summary>
    /// Action: Establece el estado de carga.
    /// </summary>
    /// <param name="isLoading">True si está cargando.</param>
    public void SetLoading(bool isLoading)
    {
        var newState = _state.Value with { IsLoading = isLoading };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Establece un mensaje de error.
    /// </summary>
    /// <param name="error">Mensaje de error.</param>
    public void SetError(string? error)
    {
        var newState = _state.Value with { Error = error };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Obtiene el título por defecto según el tipo.
    /// </summary>
    /// <param name="type">Tipo de notificación.</param>
    /// <returns>Título por defecto.</returns>
    private static string GetDefaultTitle(NotificationType type)
    {
        return type switch
        {
            NotificationType.Info => "Información",
            NotificationType.Success => "¡Éxito!",
            NotificationType.Warning => "Advertencia",
            NotificationType.Error => "Error",
            _ => "Notificación"
        };
    }

    /// <summary>
    /// Selector genérico.
    /// </summary>
    /// <typeparam name="T">Tipo del resultado.</typeparam>
    /// <param name="selector">Función de transformación.</param>
    /// <returns>Observable con el valor.</returns>
    public IObservable<T> Select<T>(Func<NotificationState, T> selector)
    {
        return _state.Select(selector).DistinctUntilChanged();
    }
}
