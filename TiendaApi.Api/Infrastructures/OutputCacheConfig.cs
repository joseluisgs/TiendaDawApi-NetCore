using Microsoft.AspNetCore.OutputCaching;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de la caché HTTP (Opción A del plan: OutputCache + ETag + 304).
/// Solo se cachean los endpoints que lo declaran con [OutputCache(...)]:
/// GET anónimos de Productos y Categorías. Invalidación por tag desde los
/// servicios (IOutputCacheStore.EvictByTagAsync) tras cada CUD, incluido GraphQL.
/// </summary>
public static class OutputCacheConfig
{
    /// <summary>
    /// Registra el middleware de caché de salida (OutputCache).
    /// Las políticas concretas (60 s + tag) se declaran por endpoint con
    /// [OutputCache(Duration = 60, Tags = new[] { ... })].
    /// </summary>
    public static IServiceCollection AddOutputCacheConfig(this IServiceCollection services)
    {
        services.AddOutputCache();
        return services;
    }

    /// <summary>
    /// Habilita la caché de salida en el pipeline. Debe llamarse antes de MapControllers.
    /// </summary>
    public static WebApplication UseOutputCacheConfig(this WebApplication app)
    {
        app.UseOutputCache();
        return app;
    }
}
