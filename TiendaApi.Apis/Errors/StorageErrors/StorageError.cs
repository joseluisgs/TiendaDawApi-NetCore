namespace TiendaApi.Apis.Errors.StorageErrors;

/// <summary>
/// Errores específicos del dominio de almacenamiento de archivos.
/// </summary>
public static class StorageError
{
    /// <summary>
    /// El archivo está vacío.
    /// </summary>
    public static ValidationError ArchivoVacio() =>
        new("El archivo está vacío", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// El archivo excede el tamaño máximo.
    /// </summary>
    public static ValidationError ArchivoMuyGrande() =>
        new("El archivo excede el tamaño máximo permitido", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Extensión de archivo no permitida.
    /// </summary>
    public static ValidationError ExtensionNoPermitida() =>
        new("Extensión de archivo no permitida", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Tipo de contenido no permitido.
    /// </summary>
    public static ValidationError TipoContenidoNoPermitido() =>
        new("Tipo de contenido no permitido", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Nombre de archivo inválido.
    /// </summary>
    public static ValidationError NombreArchivoInvalido() =>
        new("Nombre de archivo inválido", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Error al guardar el archivo.
    /// </summary>
    public static ValidationError ErrorGuardando() =>
        new("Error al guardar archivo", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Error al eliminar el archivo.
    /// </summary>
    public static ValidationError ErrorEliminando() =>
        new("Error al eliminar archivo", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío
}
