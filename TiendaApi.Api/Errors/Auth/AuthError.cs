namespace TiendaApi.Api.Errors.Auth;

/// <summary>
/// Errores de autenticación y registro (HTTP 401, 409, 400).
/// </summary>
public static class AuthError
{
    /// <summary>Crea error para credenciales inválidas.</summary>
    /// <returns>UnauthorizedError (HTTP 401).</returns>
    public static UnauthorizedError CredencialesInvalidas() =>
        UnauthorizedError.InvalidCredentials();

    /// <summary>Crea error para username duplicado.</summary>
    /// <param name="username">Nombre de usuario duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError UsernameExistente(string username) =>
        ConflictError.Duplicate("nombre de usuario", username);

    /// <summary>Crea error para email duplicado.</summary>
    /// <param name="email">Email duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError EmailExistente(string email) =>
        ConflictError.Duplicate("email", email);

    /// <summary>Crea error de validación simple.</summary>
    /// <param name="mensaje">Mensaje de error.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError Validacion(string mensaje) =>
        ValidationError.Create(mensaje);

    /// <summary>Crea error de validación con detalles por campo.</summary>
    /// <param name="errores">Diccionario de errores por campo.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}
