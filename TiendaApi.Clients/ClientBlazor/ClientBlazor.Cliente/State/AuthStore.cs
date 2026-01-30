using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace ClientBlazor.Cliente.State;

/// <summary>
/// Store de autenticación con patrón Pinia-like usando RX (BehaviorSubject).
/// Gestiona token JWT, usuario y estado de autenticación en memoria.
/// </summary>
public class AuthStore
{
    /// <summary>
    /// Estado inmutable de autenticación.
    /// </summary>
    public record AuthState(
        string? Token = null,
        string Email = "",
        string Nombre = "",
        string Role = "",
        bool IsLoading = false,
        string? Error = null
    )
    {
        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
        public bool IsAdmin => Role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        public string DisplayName => string.IsNullOrEmpty(Nombre) ? Email : Nombre;
    }

    private readonly BehaviorSubject<AuthState> _state;
    
    /// <summary>
    /// Observable del estado completo.
    /// </summary>
    public IObservable<AuthState> State => _state.AsObservable();

    /// <summary>
    /// Getter reactivo: token JWT.
    /// </summary>
    public IObservable<string?> TokenObservable => _state.Select(s => s.Token).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: indica si está autenticado.
    /// </summary>
    public IObservable<bool> IsAuthenticatedObservable => _state.Select(s => s.IsAuthenticated).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: indica si es admin.
    /// </summary>
    public IObservable<bool> IsAdminObservable => _state.Select(s => s.IsAdmin).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: email del usuario.
    /// </summary>
    public IObservable<string> EmailObservable => _state.Select(s => s.Email).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: rol del usuario.
    /// </summary>
    public IObservable<string> RoleObservable => _state.Select(s => s.Role).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: nombre para mostrar.
    /// </summary>
    public IObservable<string> DisplayNameObservable => _state.Select(s => s.DisplayName).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: indica si hay carga en progreso.
    /// </summary>
    public IObservable<bool> IsLoadingObservable => _state.Select(s => s.IsLoading).DistinctUntilChanged();

    /// <summary>
    /// Getter reactivo: mensaje de error.
    /// </summary>
    public IObservable<string?> ErrorObservable => _state.Select(s => s.Error).DistinctUntilChanged();

    /// <summary>
    /// Inicializa el store con estado inicial vacío.
    /// </summary>
    public AuthStore()
    {
        _state = new BehaviorSubject<AuthState>(new AuthState());
    }

    /// <summary>
    /// Obtiene el estado actual.
    /// </summary>
    /// <returns>Estado actual.</returns>
    public AuthState GetState() => _state.Value;

    /// <summary>
    /// Action: Establece token y usuario (login exitoso).
    /// </summary>
    /// <param name="token">Token JWT.</param>
    /// <param name="email">Email del usuario.</param>
    /// <param name="nombre">Nombre del usuario.</param>
    /// <param name="role">Rol del usuario.</param>
    public void SetAuth(string token, string email, string nombre, string role)
    {
        var newState = _state.Value with
        {
            Token = token,
            Email = email,
            Nombre = nombre,
            Role = role,
            Error = null
        };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Actualiza solo el token.
    /// </summary>
    /// <param name="token">Nuevo token.</param>
    public void SetToken(string token)
    {
        var newState = _state.Value with { Token = token };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Limpia todo (logout).
    /// </summary>
    public void Logout()
    {
        _state.OnNext(new AuthState());
    }

    /// <summary>
    /// Action: Establece estado de carga.
    /// </summary>
    /// <param name="isLoading">True si está cargando.</param>
    public void SetLoading(bool isLoading)
    {
        var newState = _state.Value with { IsLoading = isLoading };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Establece mensaje de error.
    /// </summary>
    /// <param name="error">Mensaje de error.</param>
    public void SetError(string? error)
    {
        var newState = _state.Value with { Error = error };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Action: Limpia el error.
    /// </summary>
    public void ClearError()
    {
        var newState = _state.Value with { Error = null };
        _state.OnNext(newState);
    }

    /// <summary>
    /// Selector genérico.
    /// </summary>
    /// <typeparam name="T">Tipo del resultado.</typeparam>
    /// <param name="selector">Función de transformación.</param>
    /// <returns>Observable con el valor.</returns>
    public IObservable<T> Select<T>(Func<AuthState, T> selector)
    {
        return _state.Select(selector).DistinctUntilChanged();
    }
}
