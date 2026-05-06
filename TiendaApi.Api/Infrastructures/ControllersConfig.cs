using FluentValidation;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de controladores MVC y validación FluentValidation.
/// </summary>
public static class ControllersConfig
{
    /// <summary>
    /// Configura los controladores MVC con negociación de contenido.
    /// </summary>
    public static IMvcBuilder AddMvcControllers(this IServiceCollection services)
    {
        Log.Information("📦 Configurando controladores MVC...");
        return services.AddControllers(options => {
            options.RespectBrowserAcceptHeader = true;
            options.ReturnHttpNotAcceptable = true;
        });
        //.AddXmlSerializerFormatters()
        //.AddXmlDataContractSerializerFormatters();
    }

    /// <summary>
    /// Configura FluentValidation para validaciones declarativas.
    /// </summary>
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        Log.Information("✓ Configurando FluentValidation...");
        return services.AddValidatorsFromAssemblyContaining<Program>();
    }
}
