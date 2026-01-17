using Microsoft.AspNetCore.Builder;
using Serilog;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extension methods para CORS.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Aplica la política CORS configurada (AllowAll).
    /// </summary>
    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        Log.Information("🌐 Aplicando política CORS...");
        return app.UseCors("AllowAll");
    }
}
