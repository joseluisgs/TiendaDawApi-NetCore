using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.Services.Background.Host;
using TiendaApi.Apis.Services.Background.Jobs;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de servicios de background jobs.
/// </summary>
public static class BackgroundJobsConfig
{
    /// <summary>
    /// Configura los servicios de background jobs.
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        Log.Information("🛠️ Configurando servicios de background jobs...");

        services.AddScoped<IProductoReportTask, ProductoReportTask>();
        services.AddHostedService<BackgroundJobService>();

        return services;
    }
}
