using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Mappers;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de AutoMapper.
/// </summary>
public static class AutoMapperConfig
{
    /// <summary>
    /// Configura AutoMapper con los perfiles de mapeo definidos.
    /// </summary>
    public static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        Log.Information("🔄 Configurando AutoMapper...");
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));
        return services;
    }
}
