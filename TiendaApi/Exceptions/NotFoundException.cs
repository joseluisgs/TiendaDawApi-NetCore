namespace TiendaApi.Exceptions;

/// <summary>
/// Exception thrown when a resource is not found
/// Traditional approach similar to Java/Spring Boot
/// 
/// Java equivalent: throws ResourceNotFoundException
/// Spring Boot: @ResponseStatus(HttpStatus.NOT_FOUND)
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
