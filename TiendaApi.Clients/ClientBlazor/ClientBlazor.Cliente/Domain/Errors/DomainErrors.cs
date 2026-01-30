namespace ClientBlazor.Cliente.Domain.Errors;

/// <summary>
/// Clase base abstracta para representar errores dentro del dominio del cliente.
/// Proporciona una estructura consistente para el manejo de excepciones de negocio.
/// </summary>
/// <param name="Code">Código identificador único del error (ej. AUTH_INVALID_CREDENTIALS).</param>
/// <param name="Message">Mensaje descriptivo del error para el usuario.</param>
public abstract class DomainError(string Code, string Message) : Exception(Message)
{
    /// <summary>Obtiene el código identificador del error.</summary>
    public string Code { get; } = Code;
    /// <summary>Obtiene el mensaje detallado del error.</summary>
    public new string Message { get; } = Message;
}

/// <summary>
/// Contenedor de errores predefinidos relacionados con la autenticación y autorización.
/// </summary>
public static class AuthErrors
{
    /// <summary>Error disparado cuando las credenciales no coinciden con ningún usuario activo.</summary>
    public static DomainError InvalidCredentials =>
        new AuthError("AUTH_INVALID_CREDENTIALS", "Credenciales inválidas");

    /// <summary>Error disparado cuando el usuario solicitado no existe en el sistema.</summary>
    public static DomainError UserNotFound =>
        new AuthError("AUTH_USER_NOT_FOUND", "Usuario no encontrado");

    /// <summary>Error disparado cuando el token JWT ha caducado.</summary>
    public static DomainError TokenExpired =>
        new AuthError("AUTH_TOKEN_EXPIRED", "Token expirado");

    /// <summary>Error disparado cuando el usuario no tiene los permisos necesarios (rol) para la operación.</summary>
    public static DomainError InsufficientPermissions =>
        new AuthError("AUTH_INSUFFICIENT_PERMISSIONS", "Permisos insuficientes");

    /// <summary>Error disparado cuando se intenta acceder a un recurso protegido sin haber iniciado sesión.</summary>
    public static DomainError LoginRequired =>
        new AuthError("AUTH_LOGIN_REQUIRED", "Debes iniciar sesión");

    private class AuthError(string code, string message) : DomainError(code, message);
}

/// <summary>
/// Contenedor de errores relacionados con la validación de datos de entrada.
/// </summary>
public static class ValidationErrors
{
    /// <summary>Crea un error de campo obligatorio.</summary>
    /// <param name="fieldName">Nombre del campo que falta.</param>
    /// <returns>Una instancia de <see cref="DomainError"/>.</returns>
    public static DomainError EmptyField(string fieldName) =>
        new ValidationError("VALIDATION_EMPTY_FIELD", $"El campo {fieldName} es obligatorio");

    /// <summary>Error disparado cuando el formato del correo electrónico es incorrecto.</summary>
    public static DomainError InvalidEmail =>
        new ValidationError("VALIDATION_INVALID_EMAIL", "Email inválido");

    /// <summary>Crea un error de longitud mínima.</summary>
    /// <param name="fieldName">Nombre del campo.</param>
    /// <param name="minLength">Longitud mínima requerida.</param>
    /// <returns>Una instancia de <see cref="DomainError"/>.</returns>
    public static DomainError TooShort(string fieldName, int minLength) =>
        new ValidationError("VALIDATION_TOO_SHORT", $"{fieldName} debe tener al menos {minLength} caracteres");

    private class ValidationError(string code, string message) : DomainError(code, message);
}

/// <summary>
/// Contenedor de errores de infraestructura de red y conectividad.
/// </summary>
public static class NetworkErrors
{
    /// <summary>Error general de imposibilidad de establecer conexión con la API.</summary>
    public static DomainError ConnectionFailed =>
        new NetworkError("NETWORK_CONNECTION_FAILED", "Error de conexión");

    /// <summary>Error disparado cuando el servidor de la API devuelve un código 500 o superior.</summary>
    public static DomainError ServerError =>
        new NetworkError("NETWORK_SERVER_ERROR", "Error del servidor");

    /// <summary>Error disparado cuando un recurso REST devuelve un código 404.</summary>
    public static DomainError NotFound =>
        new NetworkError("NETWORK_NOT_FOUND", "Recurso no encontrado");

    private class NetworkError(string code, string message) : DomainError(code, message);
}

/// <summary>
/// Errores genéricos no categorizados.
/// </summary>
public static class GeneralErrors
{
    /// <summary>Error disparado ante excepciones no controladas o estados inesperados.</summary>
    public static DomainError Unexpected =>
        new GeneralError("GENERAL_UNEXPECTED", "Error inesperado");

    private class GeneralError(string code, string message) : DomainError(code, message);
}
