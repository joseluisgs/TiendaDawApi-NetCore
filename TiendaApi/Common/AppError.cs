namespace TiendaApi.Common;

/// <summary>
/// Error types for Result Pattern
/// Provides structured error information without throwing exceptions
/// 
/// Java Spring Boot comparison:
/// - HttpStatus.NOT_FOUND → ErrorType.NotFound
/// - @Valid validation errors → ErrorType.Validation
/// - Business rules → ErrorType.BusinessRule
/// - Unauthorized → ErrorType.Unauthorized
/// </summary>
public class AppError
{
    public ErrorType Type { get; }
    public string Message { get; }
    public string? Details { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    private AppError(ErrorType type, string message, string? details = null, Dictionary<string, string[]>? validationErrors = null)
    {
        Type = type;
        Message = message;
        Details = details;
        ValidationErrors = validationErrors;
    }

    // Factory methods for different error types
    public static AppError NotFound(string message, string? details = null) =>
        new(ErrorType.NotFound, message, details);

    public static AppError Validation(string message, Dictionary<string, string[]>? errors = null) =>
        new(ErrorType.Validation, message, validationErrors: errors);

    public static AppError BusinessRule(string message, string? details = null) =>
        new(ErrorType.BusinessRule, message, details);

    public static AppError Unauthorized(string message = "No autorizado") =>
        new(ErrorType.Unauthorized, message);

    public static AppError Forbidden(string message = "Acceso denegado") =>
        new(ErrorType.Forbidden, message);

    public static AppError Conflict(string message, string? details = null) =>
        new(ErrorType.Conflict, message, details);

    public static AppError Internal(string message = "Error interno del servidor", string? details = null) =>
        new(ErrorType.Internal, message, details);

    public override string ToString() => 
        $"{Type}: {Message}" + (Details != null ? $" - {Details}" : "");
}

/// <summary>
/// Error type enumeration
/// Maps to HTTP status codes in controller responses
/// </summary>
public enum ErrorType
{
    NotFound,       // 404
    Validation,     // 400
    BusinessRule,   // 400
    Unauthorized,   // 401
    Forbidden,      // 403
    Conflict,       // 409
    Internal        // 500
}
