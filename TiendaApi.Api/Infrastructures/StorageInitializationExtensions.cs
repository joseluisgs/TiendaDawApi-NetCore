using Microsoft.AspNetCore.Builder;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extension methods para inicialización del directorio de almacenamiento.
/// </summary>
public static class StorageInitializationExtensions
{
    /// <summary>
    /// Inicializa el directorio de almacenamiento de archivos.
    /// Desarrollo: Borra y recrea el directorio.
    /// Producción: Solo crea si no existe.
    /// </summary>
    public static void InitializeStorage(this WebApplication app, bool isDevelopment)
    {
        // WebRootPath puede ser null si no está configurado explícitamente. Usamos ContentRootPath como respaldo.
        var rootPath = app.Environment.WebRootPath ?? app.Environment.ContentRootPath;
        var storagePath = System.IO.Path.Combine(rootPath, "wwwroot", "uploads");

        if (isDevelopment)
        {
            Log.Information("🖼️ [DESARROLLO] Preparando directorio de almacenamiento: {Path}", storagePath);
            PrepararDirectorioDesarrollo(storagePath);
        }
        else
        {
            Log.Information("🖼️ [PRODUCCIÓN] Verificando directorio de almacenamiento: {Path}", storagePath);
            AsegurarDirectorio(storagePath);
        }
    }

    /// <summary>
    /// Desarrollo: vacía el directorio (archivos y subcarpetas) y lo recrea si
    /// faltaba. Los errores van al log sin detener el arranque: el estado del
    /// disco de uploads es un detalle del entorno, no de la lógica de negocio.
    /// </summary>
    private static void PrepararDirectorioDesarrollo(string storagePath)
    {
        var storageDirectory = new System.IO.DirectoryInfo(storagePath);
        try
        {
            if (storageDirectory.Exists)
            {
                foreach (var file in storageDirectory.GetFiles())
                    file.Delete();
                foreach (var dir in storageDirectory.GetDirectories())
                    dir.Delete(true);
                Log.Information("✅ Contenido del directorio borrado");
            }

            if (!storageDirectory.Exists)
            {
                storageDirectory.Create();
                Log.Information("✅ Directorio de almacenamiento creado");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error al preparar directorio de almacenamiento");
        }
    }

    /// <summary>
    /// Producción: solo crea el directorio si no existía, sin tocar el
    /// contenido ya presente. Mismo criterio: errores en log, no en el arranque.
    /// </summary>
    private static void AsegurarDirectorio(string storagePath)
    {
        var storageDirectory = new System.IO.DirectoryInfo(storagePath);
        try
        {
            if (!storageDirectory.Exists)
            {
                storageDirectory.Create();
                Log.Information("✅ Directorio de almacenamiento creado");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error al verificar directorio de almacenamiento");
        }
    }
}
