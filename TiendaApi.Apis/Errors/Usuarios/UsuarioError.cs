namespace TiendaApi.Apis.Errors.Usuarios;

/// <summary>
/// Errores específicos del dominio de usuarios.
/// </summary>
public static class UsuarioError
{
    /// <summary>
    /// Usuario no encontrado por ID.
    /// </summary>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Usuario");

    /// <summary>
    /// Usuario no encontrado por email.
    /// </summary>
    public static NotFoundError NotFoundByEmail(string email) =>
        new($"Usuario con email '{email}' no encontrado");

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
    /// Credenciales inválidas.
    /// </summary>
    public static UnauthorizedError CredencialesInvalidas() =>
        UnauthorizedError.InvalidCredentials();

    /// <summary>
    /// Token expirado o inválido.
    /// </summary>
    public static UnauthorizedError TokenExpirado() =>
        UnauthorizedError.TokenExpired();

    /// <summary>
    /// No se puede eliminar un usuario con pedidos asociados.
    /// </summary>
    public static BusinessRuleError NoSePuedeEliminarConPedidos(long id) =>
        new($"No se puede eliminar el usuario con ID {id} porque tiene pedidos asociados");

    /// <summary>
    /// No se puede eliminar un usuario con productos asociados.
    /// </summary>
    public static BusinessRuleError NoSePuedeEliminarConProductos(long id) =>
        new($"No se puede eliminar el usuario con ID {id} porque tiene productos a la venta");

    /// <summary>
    /// Error de validación al procesar usuario.
    /// </summary>
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Error de validación con errores por campo.
    /// </summary>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}
