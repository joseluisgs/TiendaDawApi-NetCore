namespace TiendaApi.Apis.Errors;

/// <summary>
/// Excepción específica para errores de serialización de PostgreSQL (código 40001).
/// Se usa en el enfoque híbrido Serializable + Retry.
/// </summary>
public class SerializationFailureException : Exception
{
    public SerializationFailureException(string message) : base(message) { }

    public SerializationFailureException(string message, Exception innerException) : base(message, innerException) { }
}
