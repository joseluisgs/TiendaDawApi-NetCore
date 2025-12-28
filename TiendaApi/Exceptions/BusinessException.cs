namespace TiendaApi.Exceptions;

/// <summary>
/// Exception thrown for business rule violations
/// Traditional approach similar to Java/Spring Boot
/// 
/// Java equivalent: throws BusinessException or custom business exceptions
/// Spring Boot: Custom exception with @ResponseStatus(HttpStatus.BAD_REQUEST)
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }

    public BusinessException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
