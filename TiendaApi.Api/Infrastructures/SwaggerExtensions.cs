using Microsoft.AspNetCore.Builder;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extension methods para Swagger UI.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Configura Swagger UI solo en desarrollo.
    /// </summary>
    public static IApplicationBuilder UseSwaggerUI(this IApplicationBuilder app, bool isDevelopment)
    {
        if (isDevelopment)
        {
            Log.Information("📖 Habilitando Swagger UI...");
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "TiendaApi v1");
                options.RoutePrefix = string.Empty;
            });
        }
        return app;
    }
}
