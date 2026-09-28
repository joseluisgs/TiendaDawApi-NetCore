using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
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
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                // "v1" para 1.0: alinea el grupo del ApiExplorer con el SwaggerDoc "v1",
                // de lo contrario Swashbuckle descarta las operaciones de los controllers.
                options.GroupNameFormat = "'v'VVV";
            })
            .AddMvc();
        return services;
    }
}
