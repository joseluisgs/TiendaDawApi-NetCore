using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de Swagger/OpenAPI.
/// </summary>
public static class SwaggerConfig
{
    /// <summary>
    /// Configura Swagger/OpenAPI con documentación completa y seguridad JWT.
    /// </summary>
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        Log.Information("📖 Configurando Swagger/OpenAPI...");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TiendaApi - API REST Educativa",
                Version = "v1",
                Description = "API REST educativa desarrollada en .NET 10",
                Contact = new OpenApiContact
                {
                    Name = "José Luis González Sánchez",
                    Email = "joseluis.gonzalez@iesluisvives.org",
                    Url = new Uri("https://joseluisgs.dev")
                },
                License = new OpenApiLicense
                {
                    Name = "Creative Commons BY-NC-SA 4.0",
                    Url = new Uri("https://creativecommons.org/licenses/by-nc-sa/4.0/")
                }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header usando el esquema Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
