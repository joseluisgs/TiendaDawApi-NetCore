namespace TiendaApi.Api.Exceptions;

/// <summary>
/// Excepción para violaciones de reglas de negocio (HTTP 400/422).
/// </summary>
public class BusinessException : Exception
{
    /// <summary>Crea excepción con mensaje.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    public BusinessException(string message) : base(message)
    {
    }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    /// <param name="innerException">Excepción interna.</param>
    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
