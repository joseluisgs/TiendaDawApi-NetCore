using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Errors.Backup;

/// <summary>
/// Excepción interna del dominio de backup: envuelve un <see cref="DomainError"/>
/// para que los servicios de bajo nivel (codecs, lectura de ficheros) puedan
/// señalar el error concreto sin devolver un Result en cada paso.
/// El servicio la captura y la convierte en el Result de la operación.
/// </summary>
public sealed class BackupException : Exception
{
    /// <summary>Error de dominio que describe el fallo.</summary>
    public DomainError Error { get; }

    /// <summary>Crea la excepción con el error de dominio correspondiente.</summary>
    /// <param name="error">Error de dominio (400, 404 o 409 según el caso).</param>
    public BackupException(DomainError error)
        : base(error.Message)
    {
        Error = error;
    }
}
