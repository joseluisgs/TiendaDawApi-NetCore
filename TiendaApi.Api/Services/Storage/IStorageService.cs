using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Storage;

/// <summary>
/// Interfaz para operaciones de almacenamiento de archivos.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Guarda un archivo en el sistema de almacenamiento.
    /// </summary>
    /// <param name="file">Archivo a guardar.</param>
    /// <param name="folder">Carpeta destino (productos, usuarios, categorias).</param>
    /// <returns>Ruta relativa del archivo guardado.</returns>
    /// <example>
    /// var resultado = await _storageService.SaveFileAsync(archivo, "productos");
    /// if (resultado.IsSuccess) { /* guardar ruta en BD */ }
    /// </example>
    Task<Result<string, DomainError>> SaveFileAsync(IFormFile file, string folder);

    /// <summary>
    /// Elimina un archivo del sistema de almacenamiento.
    /// </summary>
    /// <param name="filename">Ruta relativa del archivo a eliminar.</param>
    /// <returns>True si se eliminó, false si no existía.</returns>
    /// <example>
    /// await _storageService.DeleteFileAsync("/images/productos/uuid.jpg");
    /// </example>
    Task<Result<bool, DomainError>> DeleteFileAsync(string filename);

    /// <summary>
    /// Verifica si un archivo existe.
    /// </summary>
    /// <param name="filename">Ruta relativa o nombre del archivo.</param>
    /// <returns>True si existe, false en caso contrario.</returns>
    bool FileExists(string filename);

    /// <summary>
    /// Obtiene la ruta física completa de un archivo.
    /// </summary>
    /// <param name="filename">Nombre del archivo o ruta relativa.</param>
    /// <returns>Ruta completa en el sistema de archivos.</returns>
    /// <example>
    /// GetFullPath("productos/uuid.jpg") // "C:\...\storage\productos\uuid.jpg"
    /// </example>
    string GetFullPath(string filename);

    /// <summary>
    /// Genera una ruta relativa formateada para almacenar en BD.
    /// </summary>
    /// <param name="filename">Nombre del archivo.</param>
    /// <param name="folder">Carpeta dentro del storage.</param>
    /// <returns>Ruta relativa formateada: /images/{carpeta}/{nombre}</returns>
    /// <example>
    /// GetRelativePath("avatar.png", "usuarios") // "/images/usuarios/avatar.png"
    /// </example>
    string GetRelativePath(string filename, string folder = "productos");
}
