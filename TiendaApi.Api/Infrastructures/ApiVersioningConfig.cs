using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de versionado de API.
/// </summary>
public static class ApiVersioningConfig
{
    /// <summary>
    /// Configura el versionado de API con versión por defecto 1.0.
    /// </summary>
    public static IServiceCollection AddApiVersioningPolicy(this IServiceCollection services)
    {
        Log.Information("🔢 Configurando API Versioning...");
        return services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });
    }
}
