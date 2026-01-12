namespace TiendaApi.Apis.Errors;

/// <summary>
/// Tipos de errores del dominio.
/// </summary>
public record DomainError(string Message, ErrorType Type, string? Details = null)
{
    private Dictionary<string, string[]>? _validationErrors;

    /// <summary>
    /// Errores de validación asociados al dominio.
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors
    {
        get => _validationErrors;
        init => _validationErrors = value;
    }

    /// <summary>
    /// Crea un error de tipo no encontrado.
    /// </summary>
    public static DomainError NotFound(string message, string? details = null) =>
        new(message, ErrorType.NotFound, details);

    /// <summary>
    /// Crea un error de tipo validación.
    /// </summary>
    public static DomainError Validation(string message, Dictionary<string, string[]>? errors = null) =>
        new(message, ErrorType.Validation, null) { ValidationErrors = errors };

    /// <summary>
    /// Crea un error de tipo regla de negocio.
    /// </summary>
    public static DomainError BusinessRule(string message, string? details = null) =>
        new(message, ErrorType.BusinessRule, details);

    /// <summary>
    /// Crea un error de tipo no autorizado.
    /// </summary>
    public static DomainError Unauthorized(string message = "No autorizado") =>
        new(message, ErrorType.Unauthorized);

    /// <summary>
    /// Crea un error de tipo prohibido.
    /// </summary>
    public static DomainError Forbidden(string message = "Acceso denegado") =>
        new(message, ErrorType.Forbidden);

    /// <summary>
    /// Crea un error de tipo conflicto.
    /// </summary>
    public static DomainError Conflict(string message, string? details = null) =>
        new(message, ErrorType.Conflict, details);

    /// <summary>
    /// Crea un error de tipo interno.
    /// </summary>
    public static DomainError Internal(string message = "Error interno del servidor", string? details = null) =>
        new(message, ErrorType.Internal, details);

    /// <summary>
    /// Representación en string del error de dominio.
    /// </summary>
    public override string ToString() => 
        $"{Type}: {Message}" + (Details != null ? $" - {Details}" : "");
}

/// <summary>
/// Enumera los tipos de errores posibles.
/// </summary>
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
