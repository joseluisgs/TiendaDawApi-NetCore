namespace TiendaApi.Exceptions;

/// <summary>
/// Excepción para recursos no encontrados.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Crea una nueva excepción de recurso no encontrado.
    /// </summary>
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Crea una nueva excepción de recurso no encontrado con inner exception.
    /// </summary>
    public NotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
