namespace TiendaApi.Api.Exceptions;

/// <summary>
/// Excepción para errores de validación de datos (HTTP 400).
/// </summary>
public class ValidationException : Exception
{
    /// <summary>Errores de validación por campo.</summary>
    public Dictionary<string, string[]> Errors { get; }

    /// <summary>Crea excepción con mensaje simple.</summary>
    /// <param name="message">Mensaje de error.</param>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>Crea excepción con errores por campo.</summary>
    /// <param name="message">Mensaje de error.</param>
    /// <param name="errors">Diccionario de errores por campo.</param>
    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje de error.</param>
    /// <param name="innerException">Excepción interna.</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
