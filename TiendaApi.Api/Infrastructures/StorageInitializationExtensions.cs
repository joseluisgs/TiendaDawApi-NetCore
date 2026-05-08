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
        var storageDirectory = new System.IO.DirectoryInfo(storagePath);

        if (isDevelopment)
        {
            Log.Information("🖼️ [DESARROLLO] Preparando directorio de almacenamiento: {Path}", storagePath);
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
        else
        {
            Log.Information("🖼️ [PRODUCCIÓN] Verificando directorio de almacenamiento: {Path}", storagePath);
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
}
