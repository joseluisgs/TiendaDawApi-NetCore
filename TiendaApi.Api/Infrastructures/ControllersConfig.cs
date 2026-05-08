using FluentValidation;
using FluentValidation.AspNetCore;
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
    /// Registra validadores y configura auto-validación en el pipeline MVC.
    /// </summary>
    /// <remarks>
    /// Data Annotations valida formato básico (requerido, rango, email). FluentValidation complementa
    /// con reglas de negocio complejas (condicionales, múltiples campos). Ambos se ejecutan antes del controller.
    /// </remarks>
    public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        Log.Information("✓ Configurando FluentValidation...");
        return services
            .AddValidatorsFromAssemblyContaining<Program>()  // Busca validadores en el ensamblado
            .AddFluentValidationAutoValidation()            // Auto-evalúa en cada request (ahorra validación manual en servicio)
            .AddFluentValidationClientsideAdapters();         // Genera scripts JS para cliente (opcional para REST)
    }
}
