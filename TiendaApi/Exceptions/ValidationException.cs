namespace TiendaApi.Exceptions;

/// <summary>
/// Excepción para errores de validación.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Diccionario de errores de validación por campo.
    /// </summary>
    public Dictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Crea una nueva excepción de validación.
    /// </summary>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Crea una nueva excepción de validación con errores específicos.
    /// </summary>
    public ValidationException(string message, Dictionary<string, string[]> errors) 
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>
    /// Crea una nueva excepción de validación con inner exception.
    /// </summary>
    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
