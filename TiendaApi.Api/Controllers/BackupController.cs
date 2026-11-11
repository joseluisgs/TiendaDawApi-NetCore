using Asp.Versioning;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Backup;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Extensions;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Backup;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de backup: exportación e importación de datos de la tienda.
///
/// 🎓 Reservas de seguridad en esta superficie (recibe y devuelve ficheros
/// completos):
/// - solo ADMIN en los tres endpoints,
/// - <c>RequestSizeLimit</c> de 10 MB en la subida,
/// - el formato y las entidades se validan ANTES de escribir la respuesta,
/// - el import informa de los errores de fila sin abortar y admite dryRun.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(Roles = UserRoles.ADMIN)]
public class BackupController(
    IBackupService service,
    ILogger<BackupController> logger
) : ControllerBase {
    /// <summary>
    /// Exporta productos, categorías y proveedores en zip, json o csv.
    /// </summary>
    /// <param name="formato">zip (defecto), json o csv.</param>
    /// <param name="entidades">Lista separada por comas: productos, categorias, proveedores.</param>
    /// <param name="incluirBorrados">Si debe incluir los registros con borrado lógico.</param>
    /// <returns>200 con el fichero adjunto, o 400 si el formato o las entidades no valen.</returns>
    [HttpGet("export")]
    [RequestSizeLimit(BackupService.MaxBytesFichero)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromQuery] string formato = "zip",
        [FromQuery] string entidades = "productos,categorias,proveedores",
        [FromQuery] bool incluirBorrados = false) {
        var lista = ParsearEntidades(entidades);
        logger.LogInformation("Exportando backup ({Formato}: {Entidades})", formato, string.Join(",", lista));

        // Las cabeceras se fijan ANTES de escribir: si el servicio falla después,
        // no se puede cambiar el estado de la respuesta.
        var (contentType, nombreFichero) = service.DatosSalida(formato, lista);
        Response.ContentType = contentType;
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{nombreFichero}\"");

        // Kestrel no admite E/S síncrona y StreamWriter/ZipArchive hacen flush
        // sincrono al cerrar: sin esto, csv y zip abortarian la conexion a mitad
        // de descarga. Se permite SOLO durante la descarga y se restaura al salir.
        var cuerpo = HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (cuerpo is not null) cuerpo.AllowSynchronousIO = true;

        try {
            var resultado = await service.ExportarAsync(Response.Body, formato, lista, incluirBorrados);

            if (resultado.IsFailure) {
                // El formato y las entidades se validan antes de escribir: el error
                // todavia se puede devolver como 400 con el shape de dominio.
                Response.ContentType = null;
                return BadRequest(new { message = resultado.Error.Message });
            }

            await Response.Body.FlushAsync();
            return new EmptyResult();
        } finally {
            if (cuerpo is not null) cuerpo.AllowSynchronousIO = false;
        }
    }

    /// <summary>
    /// Importa un backup (zip, json o csv) con upsert por clave.
    /// </summary>
    /// <param name="file">Fichero subido (multipart/form-data, campo <c>file</c>).</param>
    /// <param name="formato">zip, json o csv. Si se omite, se deduce de la extensión.</param>
    /// <param name="entidades">Entidades del fichero (imprescindible para CSV suelto).</param>
    /// <param name="dryRun">true = validar sin escribir nada.</param>
    /// <returns>200 con el informe de la importación.</returns>
    [HttpPost("import")]
    [RequestSizeLimit(BackupService.MaxBytesFichero)]
    [ProducesResponseType(typeof(ImportReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ImportReportDto>> Import(
        IFormFile file,
        [FromQuery] string? formato = null,
        [FromQuery] string entidades = "productos,categorias,proveedores",
        [FromQuery] bool dryRun = false) {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Debe subir un fichero en el campo 'file'" });

        if (file.Length > BackupService.MaxBytesFichero)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { message = $"El fichero supera {BackupService.MaxBytesFichero} bytes" });

        var formatoEfectivo = string.IsNullOrWhiteSpace(formato)
            ? FormatoDesdeNombre(file.FileName)
            : formato.Trim().ToLowerInvariant();

        logger.LogInformation(
            "Importando backup {Nombre} ({Bytes} bytes, formato {Formato}, dryRun={DryRun})",
            file.FileName, file.Length, formatoEfectivo, dryRun);

        await using var contenido = file.OpenReadStream();
        var resultado = await service.ImportarAsync(
            contenido, formatoEfectivo, ParsearEntidades(entidades), dryRun);

        return resultado.Match(
            onSuccess: informe => informe,
            onFailure: error => error.ToHttpResult<ImportReportDto>()
        );
    }

    /// <summary>
    /// Catálogo de formatos, entidades, límites y reglas del backup.
    /// </summary>
    /// <returns>200 con el catálogo.</returns>
    [HttpGet("formatos")]
    [ProducesResponseType(typeof(CatalogoFormatosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CatalogoFormatosDto>> Formatos() {
        var resultado = await service.ObtenerFormatosAsync();

        return resultado.Match(
            onSuccess: catalogo => catalogo,
            onFailure: error => error.ToHttpResult<CatalogoFormatosDto>()
        );
    }

    /// <summary>Divide la query de entidades y la pasa a minúsculas.</summary>
    private static List<string> ParsearEntidades(string entidades) =>
        (entidades ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .Distinct()
            .ToList();

    /// <summary>Deduce el formato de la extensión del fichero subido.</summary>
    private static string FormatoDesdeNombre(string? nombre) {
        var extension = System.IO.Path.GetExtension(nombre ?? string.Empty).ToLowerInvariant();
        return extension switch {
            ".json" => "json",
            ".csv" => "csv",
            _ => "zip",
        };
    }
}
