namespace TiendaApi.Apis.Errors.Auth;

/// <summary>
/// Errores específicos del dominio de autenticación.
/// </summary>
public static class AuthError
{
    /// <summary>
    /// Credenciales inválidas.
    /// </summary>
    public static UnauthorizedError CredencialesInvalidas() =>
        UnauthorizedError.InvalidCredentials();

    /// <summary>
    /// El nombre de usuario ya existe.
    /// </summary>
    public static ConflictError UsernameExistente(string username) =>
        ConflictError.Duplicate("nombre de usuario", username);

    /// <summary>
    /// El email ya existe.
    /// </summary>
    public static ConflictError EmailExistente(string email) =>
        ConflictError.Duplicate("email", email);

    /// <summary>
    /// Error de validación en datos de autenticación.
    /// </summary>
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Error de validación con errores por campo.
    /// </summary>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}
