namespace TiendaApi.Exceptions;

/// <summary>
/// Exception thrown for validation errors
/// Traditional approach similar to Java/Spring Boot
/// 
/// Java equivalent: throws ValidationException
/// Spring Boot: @Valid with MethodArgumentNotValidException
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, Dictionary<string, string[]> errors) 
        : base(message)
    {
        Errors = errors;
    }

    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
