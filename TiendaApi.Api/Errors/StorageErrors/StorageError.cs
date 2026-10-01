namespace TiendaApi.Api.Errors.StorageErrors;

/// <summary>
/// Errores de almacenamiento de archivos (HTTP 400).
/// </summary>
public static class StorageError
{
    /// <summary>Crea error cuando el archivo está vacío.</summary>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError ArchivoVacio() =>
        ValidationError.Create("El archivo está vacío");

    /// <summary>Crea error cuando el archivo excede el tamaño máximo.</summary>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError ArchivoMuyGrande() =>
        ValidationError.Create("El archivo excede el tamaño máximo permitido");

    /// <summary>Crea error cuando la extensión no está permitida.</summary>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError ExtensionNoPermitida() =>
        ValidationError.Create("Extensión de archivo no permitida");

    /// <summary>Crea error cuando el tipo de contenido no está permitido.</summary>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError TipoContenidoNoPermitido() =>
        ValidationError.Create("Tipo de contenido no permitido");

    /// <summary>Crea error cuando el nombre de archivo es inválido.</summary>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError NombreArchivoInvalido() =>
        ValidationError.Create("Nombre de archivo inválido");

    /// <summary>Crea error al guardar archivo (fallo del servidor, no del cliente).</summary>
    /// <returns>InternalError (HTTP 500).</returns>
    public static InternalError ErrorGuardando() =>
        new("Error al guardar archivo");

    /// <summary>Crea error al eliminar archivo (fallo del servidor, no del cliente).</summary>
    /// <returns>InternalError (HTTP 500).</returns>
    public static InternalError ErrorEliminando() =>
        new("Error al eliminar archivo");
}
