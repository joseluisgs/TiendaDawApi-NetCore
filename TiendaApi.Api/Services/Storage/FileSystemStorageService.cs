using System.IO;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.StorageErrors;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Services.Storage;

/// <summary>
/// Implementación de IStorageService que almacena ficheiros en el sistema de archivos local.
/// Los ficheiros se guardan en wwwroot/{Storage:UploadPath}/{folder} para acceso web directo.
/// Por defecto: wwwroot/uploads/{folder} (configurable).
/// </summary>
public class FileSystemStorageService : IStorageService
{
    private readonly string _rootPath;
    private readonly string _uploadPath;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;
    private readonly string[] _allowedContentTypes;
    private readonly ILogger<FileSystemStorageService> _logger;

    /// <summary>
    /// Crea una instancia del servicio de almacenamiento en disco.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación (sección Storage).</param>
    /// <param name="logger">Logger de la instancia.</param>
    /// <param name="env">Entorno web (proporciona la ruta de wwwroot).</param>
    public FileSystemStorageService(IConfiguration configuration, ILogger<FileSystemStorageService> logger, IWebHostEnvironment env)
    {
        _logger = logger;

        // Configuración desde appsettings.json (ruta relativa a wwwroot)
        _uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
        _maxFileSize = configuration.GetValue<long>("Storage:MaxFileSize", 5 * 1024 * 1024);
        _allowedExtensions = configuration.GetSection("Storage:AllowedExtensions").Get<string[]>()
            ?? [".jpg", ".jpeg", ".png", ".gif"];
        _allowedContentTypes = configuration.GetSection("Storage:AllowedContentTypes").Get<string[]>()
            ?? ["image/jpeg", "image/png", "image/gif"];

        // Ruta absoluta: usar WebHostEnvironment.WebRootPath (apunta a wwwroot)
        _rootPath = System.IO.Path.Combine(env.WebRootPath, _uploadPath);

        // Crear directorio si no existe
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }

        _logger.LogInformation("Storage service inicializado en: {Path}", _rootPath);
    }

    /// <summary>
    /// Genera un nombre de ficheiro único usando timestamp y GUID.
    /// </summary>
    private static string GenerateUniqueFilename(string originalFilename)
    {
        var extension = System.IO.Path.GetExtension(originalFilename).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var sanitizedName = System.IO.Path.GetFileNameWithoutExtension(originalFilename)
            .Replace(" ", "_")
            .Replace("-", "_");
        return $"{timestamp}_{uniqueId}_{sanitizedName}{extension}";
    }

    /// <summary>
    /// Valida que el ficheiro cumpla con las restricciones configuradas.
    /// </summary>
    private UnitResult<DomainError> ValidateFile(IFormFile file)
    {
        if (file is null or { Length: 0 })
        {
            return UnitResult.Failure<DomainError>(StorageError.ArchivoVacio());
        }

        if (file.Length > _maxFileSize)
        {
            return UnitResult.Failure<DomainError>(
                StorageError.ArchivoMuyGrande());
        }

        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return UnitResult.Failure<DomainError>(
                StorageError.ExtensionNoPermitida());
        }

        var contentType = file.ContentType?.ToLowerInvariant();
        if (contentType == null || !_allowedContentTypes.Any(ct => contentType.Contains(ct.Split('/')[1])))
        {
            return UnitResult.Failure<DomainError>(
                StorageError.TipoContenidoNoPermitido());
        }

        var filename = System.IO.Path.GetFileName(file.FileName);
        if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\'))
        {
            return UnitResult.Failure<DomainError>(StorageError.NombreArchivoInvalido());
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida y guarda un fichero subido en la carpeta indicada.
    /// </summary>
    /// <param name="file">Fichero subido por el cliente.</param>
    /// <param name="folder">Carpeta destino relativa dentro del almacén.</param>
    /// <returns>Ruta relativa del fichero guardado o error de dominio.</returns>
    public Task<Result<string, DomainError>> SaveFileAsync(IFormFile file, string folder)
    {
        var validation = ValidateFile(file);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result.Failure<string, DomainError>(validation.Error));
        }

        try
        {
            // Generar nombre único
            var filename = GenerateUniqueFilename(file.FileName);

            // Crear directorio destino
            var folderPath = System.IO.Path.Combine(_rootPath, folder);
            Directory.CreateDirectory(folderPath);

            // Guardar ficheiro
            var filePath = System.IO.Path.Combine(folderPath, filename);
            var relativePath = GetRelativePath(filename, folder);

            using var stream = new FileStream(filePath, FileMode.Create);
            file.CopyTo(stream);

            _logger.LogInformation("Archivo guardado: {Path}", relativePath);

            return Task.FromResult(Result.Success<string, DomainError>(relativePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando archivo");
            return Task.FromResult(Result.Failure<string, DomainError>(
                StorageError.ErrorGuardando()));
        }
    }

    /// <summary>
    /// Elimina el fichero indicado del almacén si existe.
    /// </summary>
    /// <param name="filename">Ruta relativa del fichero a eliminar.</param>
    /// <returns>true si la operación termina bien o error de dominio.</returns>
    public Task<Result<bool, DomainError>> DeleteFileAsync(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return Task.FromResult(Result.Success<bool, DomainError>(true));
        }

        try
        {
            var fullPath = GetFullPath(filename);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Archivo eliminado: {Filename}", filename);
            }

            return Task.FromResult(Result.Success<bool, DomainError>(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando archivo {Filename}", filename);
            return Task.FromResult(Result.Failure<bool, DomainError>(
                StorageError.ErrorEliminando()));
        }
    }

    /// <summary>
    /// Indica si el fichero indicado existe en el almacén.
    /// </summary>
    /// <param name="filename">Ruta relativa del fichero.</param>
    /// <returns>true si el fichero existe; false si no existe o la ruta está vacía.</returns>
    public bool FileExists(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        var fullPath = GetFullPath(filename);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Resuelve la ruta absoluta en disco de un fichero del almacén.
    /// </summary>
    /// <param name="filename">Ruta relativa o absoluta del fichero.</param>
    /// <returns>Ruta absoluta del fichero en el sistema de archivos.</returns>
    public string GetFullPath(string filename)
    {
        if (System.IO.Path.IsPathRooted(filename))
            return filename;

        var cleanFilename = filename;
        var prefix = $"/{_uploadPath}/";

        if (filename.StartsWith("/storage/", StringComparison.OrdinalIgnoreCase))
            cleanFilename = filename["/storage/".Length..];
        else if (filename.StartsWith("/storage", StringComparison.OrdinalIgnoreCase))
            cleanFilename = filename["/storage".Length..].TrimStart('/');
        else if (filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            cleanFilename = filename[prefix.Length..];

        return System.IO.Path.Combine(_rootPath, cleanFilename);
    }

    /// <summary>
    /// Construye la ruta pública (relativa) de un fichero dentro del almacén.
    /// </summary>
    /// <param name="filename">Nombre del fichero.</param>
    /// <param name="folder">Carpeta destino (por defecto "productos").</param>
    /// <returns>Ruta relativa con formato /uploads/{folder}/{filename}.</returns>
    public string GetRelativePath(string filename, string folder = "productos")
    {
        return $"/{_uploadPath}/{folder}/{filename}";
    }
}
