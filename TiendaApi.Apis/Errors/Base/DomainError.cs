namespace TiendaApi.Apis.Errors;

/// <summary>
/// Clase base abstracta para errores del dominio.
/// Implementa el patrón sealed class para type-safety.
/// </summary>
public abstract record DomainError(string Message)
{
    /// <summary>
    /// Representación en string del error de dominio.
    /// </summary>
    public override string ToString() => $"{GetType().Name}: {Message}";
}

#region Errores Base

/// <summary>
/// Error de recurso no encontrado.
/// </summary>
public sealed record NotFoundError(string Message)
    : DomainError(Message)
{
    /// <summary>
    /// Crea un error NotFound para un recurso por ID.
    /// </summary>
    public static NotFoundError FromId(long id, string resourceType = "Unknown") =>
        new($"Recurso con ID {id} no encontrado");
}

/// <summary>
/// Error de validación de datos.
/// </summary>
public sealed record ValidationError(string Message, Dictionary<string, string[]> ValidationErrors)
    : DomainError(Message)
{
    /// <summary>
    /// Crea un error de validación con errores por campo.
    /// </summary>
    public static ValidationError WithFieldErrors(Dictionary<string, string[]> fieldErrors) =>
        new("Errores de validación", fieldErrors);

    /// <summary>
    /// Crea un error de validación simple sin errores por campo específicos.
    /// NOTA: En C#, new Dictionary&lt;string, string[]&gt;() crea un diccionario vacío.
    /// Equivale a usar {} en otros contextos.
    /// </summary>
    public static ValidationError Create(string message) =>
        new(message, new Dictionary<string, string[]>());  // new Dictionary<string, string[]>() = diccionario vacío
}

/// <summary>
/// Error de regla de negocio violada.
/// </summary>
public sealed record BusinessRuleError(string Message)
    : DomainError(Message) { }

/// <summary>
/// Error de no autorizado (authentication).
/// </summary>
public sealed record UnauthorizedError(string Message = "No autorizado")
    : DomainError(Message)
{
    /// <summary>
    /// Crea un error de credenciales inválidas.
    /// </summary>
    public static UnauthorizedError InvalidCredentials() => new("Credenciales inválidas");

    /// <summary>
    /// Crea un error de token expirado.
    /// </summary>
    public static UnauthorizedError TokenExpired() => new("Token expirado o inválido");
}

/// <summary>
/// Error de prohibido (authorization).
/// </summary>
public sealed record ForbiddenError(string Message = "Acceso denegado")
    : DomainError(Message)
{
    /// <summary>
    /// Crea un error de acceso prohibido para un recurso.
    /// </summary>
    public static ForbiddenError NotOwner(string resourceType = "recurso", long? resourceId = null) =>
        new(resourceId != null
            ? $"No tienes permisos para acceder a este {resourceType} (ID: {resourceId})"
            : $"No tienes permisos para acceder a este {resourceType}");
}

/// <summary>
/// Error de conflicto (recursos duplicados o inconsistentes).
/// </summary>
public sealed record ConflictError(string Message)
    : DomainError(Message)
{
    /// <summary>
    /// Crea un error de recurso duplicado.
    /// </summary>
    public static ConflictError Duplicate(string resourceType, string value) =>
        new($"Ya existe un {resourceType} con el valor '{value}'");
}

/// <summary>
/// Error interno del servidor.
/// </summary>
public sealed record InternalError(string Message = "Error interno del servidor")
    : DomainError(Message) { }

#endregion
