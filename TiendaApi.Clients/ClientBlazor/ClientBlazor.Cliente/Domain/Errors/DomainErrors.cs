namespace ClientBlazor.Cliente.Domain.Errors;

/// <summary>
/// Error base del dominio cliente.
/// Patrón consistente con la API.
/// </summary>
public abstract class DomainError(string Code, string Message) : Exception(Message)
{
    public string Code { get; } = Code;
    public new string Message { get; } = Message;
}

/// <summary>
/// Errores de autenticación.
/// </summary>
public static class AuthErrors
{
    public static DomainError InvalidCredentials =>
        new AuthError("AUTH_INVALID_CREDENTIALS", "Credenciales inválidas");

    public static DomainError UserNotFound =>
        new AuthError("AUTH_USER_NOT_FOUND", "Usuario no encontrado");

    public static DomainError TokenExpired =>
        new AuthError("AUTH_TOKEN_EXPIRED", "Token expirado");

    public static DomainError InsufficientPermissions =>
        new AuthError("AUTH_INSUFFICIENT_PERMISSIONS", "Permisos insuficientes");

    public static DomainError LoginRequired =>
        new AuthError("AUTH_LOGIN_REQUIRED", "Debes iniciar sesión");

    private class AuthError(string code, string message) : DomainError(code, message);
}

/// <summary>
/// Errores de validación.
/// </summary>
public static class ValidationErrors
{
    public static DomainError EmptyField(string fieldName) =>
        new ValidationError("VALIDATION_EMPTY_FIELD", $"El campo {fieldName} es obligatorio");

    public static DomainError InvalidEmail =>
        new ValidationError("VALIDATION_INVALID_EMAIL", "Email inválido");

    public static DomainError TooShort(string fieldName, int minLength) =>
        new ValidationError("VALIDATION_TOO_SHORT", $"{fieldName} debe tener al menos {minLength} caracteres");

    public static DomainError TooLong(string fieldName, int maxLength) =>
        new ValidationError("VALIDATION_TOO_LONG", $"{fieldName} no puede tener más de {maxLength} caracteres");

    private class ValidationError(string code, string message) : DomainError(code, message);
}

/// <summary>
/// Errores de red/conexión.
/// </summary>
public static class NetworkErrors
{
    public static DomainError ConnectionFailed =>
        new NetworkError("NETWORK_CONNECTION_FAILED", "Error de conexión");

    public static DomainError Timeout =>
        new NetworkError("NETWORK_TIMEOUT", "Tiempo de espera agotado");

    public static DomainError ServerError =>
        new NetworkError("NETWORK_SERVER_ERROR", "Error del servidor");

    public static DomainError NotFound =>
        new NetworkError("NETWORK_NOT_FOUND", "Recurso no encontrado");

    private class NetworkError(string code, string message) : DomainError(code, message);
}

/// <summary>
/// Errores generales.
/// </summary>
public static class GeneralErrors
{
    public static DomainError Unexpected =>
        new GeneralError("GENERAL_UNEXPECTED", "Error inesperado");

    public static DomainError OperationCancelled =>
        new GeneralError("GENERAL_OPERATION_CANCELLED", "Operación cancelada");

    private class GeneralError(string code, string message) : DomainError(code, message);
}