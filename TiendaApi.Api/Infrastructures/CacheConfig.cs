using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using StackExchange.Redis;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de caché.
/// </summary>
public static class CacheConfig
{
    /// <summary>
    /// Configura el servicio de caché.
    /// Desarrollo: MemoryCache.
    /// Producción: Redis.
    /// </summary>
    public static IServiceCollection AddCache(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            Log.Information("💾 Configurando caché en memoria (desarrollo local)...");
            services.AddMemoryCache();
            services.TryAddSingleton<ICacheService, MemoryCacheService>();
        }
        else
        {
            Log.Information("💾 Configurando caché Redis (producción)...");
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379";
                options.InstanceName = "TiendaApi:";
            });
            services.TryAddSingleton<ICacheService, RedisCacheService>();
        }

        return services;
    }
}
