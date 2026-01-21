using Microsoft.AspNetCore.Mvc;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador para servir archivos estáticos (imágenes de productos).
/// </summary>
[ApiController]
[Route("storage")]
public class StorageController(
    IWebHostEnvironment environment,
    ILogger<StorageController> logger
) : ControllerBase
{
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<StorageController> _logger = logger;

    /// <summary>
    /// Obtiene un archivo del almacenamiento local.
    /// </summary>
    /// <param name="path">Ruta del archivo dentro de images/ (ej: productos/filename.jpg).</param>
    /// <returns>El archivo como stream o error 404.</returns>
    [HttpGet("{**path}")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public IActionResult GetFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return NotFound();
        }

        try
        {
            var basePath = System.IO.Path.Combine(_environment.ContentRootPath, "wwwroot");
            var fullPath = System.IO.Path.Combine(basePath, path);

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Archivo no encontrado: {Path}", fullPath);
                return NotFound(new { error = "Archivo no encontrado", path });
            }

            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("Archivo demasiado grande: {Path} ({Size} bytes)", path, fileInfo.Length);
                return BadRequest(new { error = "Archivo demasiado grande" });
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = GetContentType(fileInfo.Extension);

            _logger.LogInformation("Sirviendo archivo: {Path}", path);
            return File(fileStream, contentType);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Acceso denegado al archivo: {Path}", path);
            return Forbid();
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error leyendo archivo: {Path}", path);
            return StatusCode(500, new { error = "Error leyendo archivo", details = ex.Message });
        }
    }

    /// <summary>
    /// Determina el tipo de contenido MIME según la extensión del archivo.
    /// </summary>
    /// <param name="extension">Extensión del archivo (ej: ".jpg", ".png").</param>
    /// <returns>Tipo MIME correspondiente (ej: "image/jpeg").</returns>
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}
