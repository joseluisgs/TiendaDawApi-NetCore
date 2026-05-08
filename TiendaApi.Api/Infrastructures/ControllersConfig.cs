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
    /// <remarks>
    /// <para><b>OPCIONES DE CONFIGURACIÓN:</b></para>
    /// 
    /// <para><b>1. AddValidatorsFromAssemblyContaining&lt;Program&gt;():</b></para>
    /// <para>   - Escanea el ensamblado especificado en busca de clases que heredan de AbstractValidator&lt;T&gt;</para>
    /// <para>   - Registra todos los validadores en el contenedor de dependencias</para>
    /// <para>   - <b>NO</b> ejecuta la validación automáticamente en el pipeline</para>
    /// <para>   - Los validadores deben llamarse manualmente desde los services/controllers</para>
    /// <para>   - Útil cuando se necesita control manual del proceso de validación</para>
    /// 
    /// <para><b>2. AddFluentValidation() (Auto Validation):</b></para>
    /// <para>   - Añade un filtro que ejecuta automáticamente FluentValidation antes de cada action</para>
    /// <para>   - Si la validación falla, devuelve automáticamente 400 Bad Request con los errores</para>
    /// <para>   - Funciona con [ApiController] y los validadores registrados</para>
    /// <para>   - <b>Recomendado</b> para la mayoría de APIs REST (equivale al método AddFluentValidationAutoValidation de versiones posteriores)</para>
    /// 
    /// <para><b>3. AddFluentValidationClientsideAdapters():</b></para>
    /// <para>   - Registra adapters para generar validación cliente-side (JavaScript)</para>
    /// <para>   - Genera scripts de validación para usar en Blazor, MVC, etc.</para>
    /// <para>   - NO necesario para APIs REST puras</para>
    /// 
    /// <para><b>COMBINACIONES COMUNES:</b></para>
    /// <para>   - Solo validadores: services.AddValidatorsFromAssemblyContaining&lt;Program&gt;()</para>
    /// <para>   - Validadores + auto: services.AddValidatorsFromAssembly&lt;Program&gt;().AddFluentValidation()</para>
    /// <para>   - Completo (API + UI): services.AddValidatorsFromAssembly&lt;Program&gt;().AddFluentValidation().AddFluentValidationClientsideAdapters()</para>
    /// </remarks>
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        Log.Information("✓ Configurando FluentValidation...");
        return services
            .AddValidatorsFromAssemblyContaining<Program>()
            .AddFluentValidation();
    }
}
