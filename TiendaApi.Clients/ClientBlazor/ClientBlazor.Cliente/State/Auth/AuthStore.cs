using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace ClientBlazor.Cliente.State.Auth;

/// <summary>
/// Implementación concreta del almacén de autenticación usando BehaviorSubject.
/// </summary>
public class AuthStore : IAuthStore
{
    /// <summary>
    /// Representa el estado inmutable de la autenticación.
    /// </summary>
    public record AuthState
    {
        public string? Token { get; init; } = null;
        public string Email { get; init; } = "";
        public string Nombre { get; init; } = "";
        public string Role { get; init; } = "";
        public bool IsLoading { get; init; } = false;
        public string? Error { get; init; } = null;

        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
        public bool IsAdmin => Role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);
        public string DisplayName => string.IsNullOrEmpty(Nombre) ? Email : Nombre;
    }

    private readonly BehaviorSubject<AuthState> _state;
    
    /// <inheritdoc cref="IAuthStore.State" />
    public IObservable<AuthState> State => _state.AsObservable();

    /// <inheritdoc cref="IAuthStore.TokenObservable" />
    public IObservable<string?> TokenObservable => _state.Select(s => s.Token).DistinctUntilChanged();

    /// <inheritdoc cref="IAuthStore.IsAuthenticatedObservable" />
    public IObservable<bool> IsAuthenticatedObservable => _state.Select(s => s.IsAuthenticated).DistinctUntilChanged();

    /// <inheritdoc cref="IAuthStore.IsAdminObservable" />
    public IObservable<bool> IsAdminObservable => _state.Select(s => s.IsAdmin).DistinctUntilChanged();

    /// <inheritdoc cref="IAuthStore.EmailObservable" />
    public IObservable<string> EmailObservable => _state.Select(s => s.Email).DistinctUntilChanged();

    /// <inheritdoc cref="IAuthStore.RoleObservable" />
    public IObservable<string> RoleObservable => _state.Select(s => s.Role).DistinctUntilChanged();

    /// <inheritdoc cref="IAuthStore.DisplayNameObservable" />
    public IObservable<string> DisplayNameObservable => _state.Select(s => s.DisplayName).DistinctUntilChanged();

    /// <inheritdoc cref="IAuthStore.IsLoadingObservable" />
    public IObservable<bool> IsLoadingObservable => _state.Select(s => s.IsLoading).DistinctUntilChanged();

    /// <inheritdoc cref="IAuthStore.ErrorObservable" />
    public IObservable<string?> ErrorObservable => _state.Select(s => s.Error).DistinctUntilChanged();

    public AuthStore()
    {
        _state = new BehaviorSubject<AuthState>(new AuthState());
    }

    /// <inheritdoc cref="IAuthStore.GetState" />
    public AuthState GetState() => _state.Value;

    /// <inheritdoc cref="IAuthStore.SetAuth(string, string, string, string)" />
    public void SetAuth(string token, string email, string nombre, string role)
    {
        _state.OnNext(_state.Value with
        {
            Token = token,
            Email = email,
            Nombre = nombre,
            Role = role,
            Error = null
        });
    }

    /// <inheritdoc cref="IAuthStore.SetToken(string)" />
    public void SetToken(string token)
    {
        _state.OnNext(_state.Value with { Token = token });
    }

    /// <inheritdoc cref="IAuthStore.Logout" />
    public void Logout()
    {
        _state.OnNext(new AuthState());
    }

    /// <inheritdoc cref="IAuthStore.SetLoading(bool)" />
    public void SetLoading(bool isLoading)
    {
        _state.OnNext(_state.Value with { IsLoading = isLoading });
    }

    /// <inheritdoc cref="IAuthStore.SetError(string?)" />
    public void SetError(string? error)
    {
        _state.OnNext(_state.Value with { Error = error });
    }

    /// <inheritdoc cref="IAuthStore.ClearError" />
    public void ClearError()
    {
        _state.OnNext(_state.Value with { Error = null });
    }

    /// <inheritdoc cref="IAuthStore.Select{T}(Func{AuthState, T})" />
    public IObservable<T> Select<T>(Func<AuthState, T> selector)
    {
        return _state.Select(selector).DistinctUntilChanged();
    }
}