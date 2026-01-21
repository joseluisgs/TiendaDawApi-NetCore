using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services.Background.Host;
using TiendaApi.Api.Services.Background.Jobs;

namespace TiendaApi.Api.Infrastructures;

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
