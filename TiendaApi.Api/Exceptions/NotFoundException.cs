namespace TiendaApi.Api.Exceptions;

/// <summary>
/// Excepción para recursos no encontrados (HTTP 404).
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>Crea excepción con mensaje.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    /// <param name="innerException">Excepción interna.</param>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
