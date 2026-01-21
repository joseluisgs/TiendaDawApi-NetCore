namespace TiendaApi.Api.Exceptions;

/// <summary>
/// Excepción para errores de serialización en PostgreSQL (HTTP 500).
/// Se usa en el enfoque híbrido Serializable + Retry.
/// </summary>
public class SerializationFailureException : Exception
{
    /// <summary>Crea excepción con mensaje.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    public SerializationFailureException(string message) : base(message) { }

    /// <summary>Crea excepción con inner exception.</summary>
    /// <param name="message">Mensaje descriptivo.</param>
    /// <param name="innerException">Excepción interna.</param>
    public SerializationFailureException(string message, Exception innerException) : base(message, innerException) { }
}
