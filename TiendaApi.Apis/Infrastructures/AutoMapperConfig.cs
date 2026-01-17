using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.Mappers;

namespace TiendaApi.Apis.Infrastructures;

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
        return services.AddAutoMapper(typeof(MappingProfile), typeof(PedidoProfile));
    }
}
