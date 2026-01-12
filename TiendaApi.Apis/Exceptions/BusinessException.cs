namespace TiendaApi.Apis.Exceptions;

/// <summary>
/// Excepción para violaciones de reglas de negocio.
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// Crea una nueva excepción de negocio.
    /// </summary>
    public BusinessException(string message) : base(message)
    {
    }

    /// <summary>
    /// Crea una nueva excepción de negocio con inner exception.
    /// </summary>
    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
