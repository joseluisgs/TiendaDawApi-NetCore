using System.IO.Compression;
using System.Text;
using TiendaApi.Api.Errors.Backup;

namespace TiendaApi.Api.Services.Backup;

/// <summary>
/// Lector y escritor de backups ZIP con la BCL, sin dependencias externas.
///
/// 🎓 Protecciones en la lectura (un ZIP es un fichero que viene de fuera):
/// - nº máximo de entradas,
/// - tamaño descomprimido máximo, comprobado ANTES (Length declarado) y
///   DURANTE la lectura (por si el declarado miente),
/// - solo se devuelven las entradas; el servicio elige las que le interesan.
/// </summary>
public static class ZipCodec
{
    /// <summary>Número máximo de ficheros dentro del ZIP.</summary>
    public const int MaxEntradas = 50;

    /// <summary>Tamaño máximo descomprimido (50 MB).</summary>
    public const int MaxBytesDescomprimidos = 50 * 1024 * 1024;

    /// <summary>Una entrada del ZIP ya leída en memoria.</summary>
    public record EntradaZip(string Nombre, string Contenido);

    /// <summary>
    /// Abre un ZIP en modo escritura sobre la salida indicada (sin cerrar el stream).
    /// </summary>
    public static ZipArchive Crear(Stream salida) =>
        new(salida, ZipArchiveMode.Create, leaveOpen: true);

    /// <summary>
    /// Lee todas las entradas de texto del ZIP aplicando los límites de seguridad.
    /// Lanza <see cref="BackupException"/> si el fichero no es admisible.
    /// </summary>
    public static IReadOnlyList<EntradaZip> Leer(Stream entrada)
    {
        using var archivo = new ZipArchive(entrada, ZipArchiveMode.Read, leaveOpen: true);

        if (archivo.Entries.Count > MaxEntradas)
            throw new BackupException(BackupError.DemasiadoGrande(
                $"{archivo.Entries.Count} entradas, máximo {MaxEntradas}"));

        // 1º límite: lo que declaran las cabeceras del ZIP, sin descomprimir nada.
        var totalDeclarado = archivo.Entries.Sum(e => e.Length);
        if (totalDeclarado > MaxBytesDescomprimidos)
            throw new BackupException(BackupError.DemasiadoGrande(
                $"{totalDeclarado} bytes descomprimidos, máximo {MaxBytesDescomprimidos}"));

        var resultados = new List<EntradaZip>();
        var totalLeido = 0;

        foreach (var entradaZip in archivo.Entries)
        {
            // Los directorios terminan en barra y no aportan contenido.
            if (entradaZip.FullName.EndsWith('/') || entradaZip.Length == 0)
                continue;

            using var flujo = entradaZip.Open();
            using var memoria = new MemoryStream();
            var buffer = new byte[8192];
            int leidos;

            // 2º límite: lo que se lee de verdad, por si el declarado miente.
            while ((leidos = flujo.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalLeido += leidos;
                if (totalLeido > MaxBytesDescomprimidos)
                    throw new BackupException(BackupError.DemasiadoGrande(
                        $"{totalLeido} bytes descomprimidos, máximo {MaxBytesDescomprimidos}"));

                memoria.Write(buffer, 0, leidos);
            }

            resultados.Add(new EntradaZip(
                entradaZip.FullName,
                Encoding.UTF8.GetString(memoria.ToArray())));
        }

        return resultados;
    }
}
