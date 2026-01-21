using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services.Storage;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de almacenamiento de archivos.
/// </summary>
public static class StorageConfig
{
    /// <summary>
    /// Configura el servicio de almacenamiento de archivos locales.
    /// </summary>
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        Log.Information("🖼️ Configurando servicio de almacenamiento...");
        return services.AddScoped<IStorageService, FileSystemStorageService>();
    }
}
