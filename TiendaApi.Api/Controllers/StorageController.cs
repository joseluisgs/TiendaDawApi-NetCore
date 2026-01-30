using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Services.Storage;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para la gestión de archivos y recursos multimedia.
/// Proporciona acceso a imágenes de productos y otros recursos estáticos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StorageController(
    IStorageService storageService,
    ILogger<StorageController> logger
) : ControllerBase
{
    /// <summary>
    /// Recupera un archivo del sistema de almacenamiento mediante su nombre.
    /// </summary>
    /// <param name="fileName">Nombre único del archivo almacenado.</param>
    /// <returns>El flujo de datos del archivo con su tipo de contenido.</returns>
    [HttpGet("{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(string fileName)
    {
        logger.LogInformation("Solicitando archivo: {FileName}", fileName);

        var resultado = await storageService.GetFileAsync(fileName);

        return resultado.Match(
            onSuccess: file => File(file.Stream, file.ContentType, file.FileName),
            onFailure: error => NotFound(new { message = error.Message })
        );
    }
}