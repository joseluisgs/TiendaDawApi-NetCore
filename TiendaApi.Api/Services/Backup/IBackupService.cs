using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Backup;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Backup;

/// <summary>
/// Contrato del servicio de backup (export, import y catálogo de formatos).
///
/// 🎓 Diseño: el export ESCRIBE en un stream (no materializa el fichero en
/// memoria) y el import LEE de otro, con límites de tamaño aplicados por el
/// codec. El servicio decide el formato, valida antes de escribir y acumula
/// los errores de fila sin abortar la operación.
/// </summary>
public interface IBackupService {
    /// <summary>
    /// Metadatos de la salida (tipo de contenido y nombre del fichero) para que
    /// el controlador fije las cabeceras ANTES de escribir en la respuesta.
    /// </summary>
    /// <param name="formato">zip, json o csv.</param>
    /// <param name="entidades">Entidades pedidas.</param>
    /// <returns>Tipo de contenido y nombre sugerido del fichero.</returns>
    (string ContentType, string NombreFichero) DatosSalida(string formato, IReadOnlyList<string> entidades);

    /// <summary>
    /// Escribe el backup en el stream indicado. Valida formato y entidades antes
    /// de escribir una sola línea, para no dejar la respuesta a medias.
    /// </summary>
    /// <param name="salida">Stream de la respuesta HTTP.</param>
    /// <param name="formato">zip, json o csv.</param>
    /// <param name="entidades">productos, categorias y/o proveedores.</param>
    /// <param name="incluirBorrados">Si debe incluir los registros con soft-delete.</param>
    /// <returns>Vacío si se escribió, o el error que impidió empezar.</returns>
    Task<UnitResult<DomainError>> ExportarAsync(
        Stream salida, string formato, IReadOnlyList<string> entidades, bool incluirBorrados);

    /// <summary>
    /// Importa un backup. Aplica upsert por clave y acumula los errores de fila
    /// sin abortar; con dryRun no escribe nada y devuelve el mismo informe.
    /// </summary>
    /// <param name="fichero">Stream con el fichero subido.</param>
    /// <param name="formato">Formato del fichero (se detecta si no se indica).</param>
    /// <param name="entidades">Entidades a importar (para CSV, que va sin nombres de fichero).</param>
    /// <param name="dryRun">true = solo validar, no escribir.</param>
    /// <returns>Informe de la importación.</returns>
    Task<Result<ImportReportDto, DomainError>> ImportarAsync(
        Stream fichero, string formato, IReadOnlyList<string> entidades, bool dryRun);

    /// <summary>Catálogo de formatos, entidades, límites y reglas.</summary>
    Task<Result<CatalogoFormatosDto, DomainError>> ObtenerFormatosAsync();
}
