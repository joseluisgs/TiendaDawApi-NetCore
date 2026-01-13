using CSharpFunctionalExtensions;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Storage;

/// <summary>
/// Interfaz para el servicio de almacenamiento de archivos.
/// Maneja operaciones de guardado y eliminación de ficheiros en el sistema de archivos.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Guarda un ficheiro en el almacenamiento.
    /// </summary>
    /// <param name="file">El ficheiro a guardar.</param>
    /// <param name="folder">Carpeta destino dentro de storage (ej: "productos").</param>
    /// <returns>Result con el nombre del ficheiro guardado o error.</returns>
    Task<Result<string, DomainError>> SaveFileAsync(IFormFile file, string folder);

    /// <summary>
    /// Elimina un ficheiro del almacenamiento.
    /// </summary>
    /// <param name="filename">Nombre del ficheiro a eliminar (ruta relativa).</param>
    /// <returns>Result con éxito o error.</returns>
    Task<Result<bool, DomainError>> DeleteFileAsync(string filename);

    /// <summary>
    /// Verifica si un ficheiro existe en el almacenamiento.
    /// </summary>
    /// <param name="filename">Nombre del ficheiro a verificar.</param>
    /// <returns>True si existe, false en caso contrario.</returns>
    bool FileExists(string filename);

    /// <summary>
    /// Obtiene la ruta completa de un ficheiro.
    /// </summary>
    /// <param name="filename">Nombre del ficheiro.</param>
    /// <returns>Ruta completa del ficheiro.</returns>
    string GetFullPath(string filename);

    /// <summary>
    /// Obtiene la ruta relativa para almacenar en la base de datos.
    /// </summary>
    /// <param name="filename">Nombre del ficheiro.</param>
    /// <param name="folder">Carpeta dentro de storage (por defecto: "productos").</param>
    /// <returns>Ruta relativa (ej: /images/productos/uuid.jpg).</returns>
    string GetRelativePath(string filename, string folder = "productos");
}
