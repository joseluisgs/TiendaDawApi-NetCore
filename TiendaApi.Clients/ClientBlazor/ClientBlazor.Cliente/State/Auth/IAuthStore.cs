using System.Reactive.Linq;
using static ClientBlazor.Cliente.State.Auth.AuthStore;

namespace ClientBlazor.Cliente.State.Auth;

/// <summary>
/// Define el contrato para el almacén de estado de autenticación.
/// Gestiona la información del usuario, el token JWT y el estado reactivo de la sesión.
/// </summary>
public interface IAuthStore
{
    /// <summary>
    /// Obtiene el observable del estado completo de autenticación.
    /// </summary>
    IObservable<AuthState> State { get; }

    /// <summary>
    /// Obtiene un observable que emite el token JWT actual o null si no hay sesión.
    /// </summary>
    IObservable<string?> TokenObservable { get; }

    /// <summary>
    /// Obtiene un observable que emite true si el usuario está autenticado.
    /// </summary>
    IObservable<bool> IsAuthenticatedObservable { get; }

    /// <summary>
    /// Obtiene un observable que emite true si el usuario tiene el rol de administrador.
    /// </summary>
    IObservable<bool> IsAdminObservable { get; }

    /// <summary>
    /// Obtiene un observable con el correo electrónico del usuario.
    /// </summary>
    IObservable<string> EmailObservable { get; }

    /// <summary>
    /// Obtiene un observable con el rol del usuario.
    /// </summary>
    IObservable<string> RoleObservable { get; }

    /// <summary>
    /// Obtiene un observable con el nombre a mostrar del usuario.
    /// </summary>
    IObservable<string> DisplayNameObservable { get; }

    /// <summary>
    /// Obtiene un observable que indica si hay una operación de autenticación en curso.
    /// </summary>
    IObservable<bool> IsLoadingObservable { get; }

    /// <summary>
    /// Obtiene un observable con el mensaje de error actual, si existe.
    /// </summary>
    IObservable<string?> ErrorObservable { get; }
    
    /// <summary>
    /// Recupera una instantánea del estado actual de autenticación.
    /// </summary>
    /// <returns>El objeto <see cref="AuthState"/> actual.</returns>
    AuthState GetState();

    /// <summary>
    /// Establece los datos de autenticación tras un inicio de sesión exitoso.
    /// </summary>
    /// <param name="token">Token JWT emitido por la API.</param>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="nombre">Nombre completo o nombre de usuario.</param>
    /// <param name="role">Rol asignado al usuario.</param>
    void SetAuth(string token, string email, string nombre, string role);

    /// <summary>
    /// Actualiza únicamente el token JWT en el estado.
    /// </summary>
    /// <param name="token">El nuevo token JWT.</param>
    void SetToken(string token);

    /// <summary>
    /// Finaliza la sesión actual reseteando el estado a sus valores por defecto.
    /// </summary>
    void Logout();

    /// <summary>
    /// Establece el estado de carga de la autenticación.
    /// </summary>
    /// <param name="isLoading">True si se está realizando una operación asíncrona.</param>
    void SetLoading(bool isLoading);

    /// <summary>
    /// Establece un mensaje de error en el estado.
    /// </summary>
    /// <param name="error">Mensaje descriptivo del error.</param>
    void SetError(string? error);

    /// <summary>
    /// Limpia cualquier mensaje de error presente en el estado.
    /// </summary>
    void ClearError();

    /// <summary>
    /// Permite seleccionar una parte específica del estado de forma reactiva.
    /// </summary>
    /// <typeparam name="T">Tipo del valor seleccionado.</typeparam>
    /// <param name="selector">Función de selección.</param>
    /// <returns>Un observable que emite el valor seleccionado cuando cambia.</returns>
    IObservable<T> Select<T>(Func<AuthState, T> selector);
}
