namespace TiendaApi.Api.Errors.Usuarios;

/// <summary>
/// Errores del dominio de usuarios (HTTP 404, 409, 401, 400).
/// </summary>
public static class UsuarioError
{
    /// <summary>Crea error para usuario no encontrado.</summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Usuario");

    /// <summary>Crea error para email no encontrado.</summary>
    /// <param name="email">Email no encontrado.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError NotFoundByEmail(string email) =>
        new($"Usuario con email '{email}' no encontrado");

    /// <summary>Crea error para username duplicado.</summary>
    /// <param name="username">Username duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError UsernameExistente(string username) =>
        ConflictError.Duplicate("nombre de usuario", username);

    /// <summary>Crea error para email duplicado.</summary>
    /// <param name="email">Email duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError EmailExistente(string email) =>
        ConflictError.Duplicate("email", email);

    /// <summary>Crea error para credenciales inválidas.</summary>
    /// <returns>UnauthorizedError (HTTP 401).</returns>
    public static UnauthorizedError CredencialesInvalidas() =>
        UnauthorizedError.InvalidCredentials();

    /// <summary>Crea error para token expirado.</summary>
    /// <returns>UnauthorizedError (HTTP 401).</returns>
    public static UnauthorizedError TokenExpirado() =>
        UnauthorizedError.TokenExpired();

    /// <summary>Crea error si el usuario tiene pedidos asociados.</summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>BusinessRuleError (HTTP 400).</returns>
    public static BusinessRuleError NoSePuedeEliminarConPedidos(long id) =>
        new($"No se puede eliminar el usuario con ID {id} porque tiene pedidos asociados");

    /// <summary>Crea error si el usuario tiene productos a la venta.</summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>BusinessRuleError (HTTP 400).</returns>
    public static BusinessRuleError NoSePuedeEliminarConProductos(long id) =>
        new($"No se puede eliminar el usuario con ID {id} porque tiene productos a la venta");

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
