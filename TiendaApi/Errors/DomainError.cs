namespace TiendaApi.Errors;

public record DomainError(string Message, ErrorType Type, string? Details = null)
{
    private Dictionary<string, string[]>? _validationErrors;

    public Dictionary<string, string[]>? ValidationErrors
    {
        get => _validationErrors;
        init => _validationErrors = value;
    }

    public static DomainError NotFound(string message, string? details = null) =>
        new(message, ErrorType.NotFound, details);

    public static DomainError Validation(string message, Dictionary<string, string[]>? errors = null) =>
        new(message, ErrorType.Validation, null) { ValidationErrors = errors };

    public static DomainError BusinessRule(string message, string? details = null) =>
        new(message, ErrorType.BusinessRule, details);

    public static DomainError Unauthorized(string message = "No autorizado") =>
        new(message, ErrorType.Unauthorized);

    public static DomainError Forbidden(string message = "Acceso denegado") =>
        new(message, ErrorType.Forbidden);

    public static DomainError Conflict(string message, string? details = null) =>
        new(message, ErrorType.Conflict, details);

    public static DomainError Internal(string message = "Error interno del servidor", string? details = null) =>
        new(message, ErrorType.Internal, details);

    public override string ToString() => 
        $"{Type}: {Message}" + (Details != null ? $" - {Details}" : "");
}

public enum ErrorType
{
    NotFound,
    Validation,
    BusinessRule,
    Unauthorized,
    Forbidden,
    Conflict,
    Internal
}
