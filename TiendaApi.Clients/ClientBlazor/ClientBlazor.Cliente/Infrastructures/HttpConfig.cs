using ClientBlazor.Cliente.Configuration;
using ClientBlazor.Cliente.Infrastructures.Handlers;

namespace ClientBlazor.Cliente.Infrastructures;

/// <summary>
/// Proporciona métodos de extensión para configurar la infraestructura base de comunicación HTTP.
/// </summary>
public static class HttpConfig
{
    /// <summary>
    /// Registra el manejador de cabeceras de autorización y el cliente HTTP base.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <returns>Contenedor de servicios actualizado.</returns>
    public static IServiceCollection AddHttpInfrastructure(this IServiceCollection services)
    {
        // Registrar el interceptor de seguridad para inyección automática de JWT
        services.AddTransient<AuthHeaderHandler>();
        
        // Registrar el cliente HTTP estándar apuntando a la base de la API
        services.AddScoped(sp => new HttpClient 
        { 
            BaseAddress = new Uri(AppConfig.ApiBaseUrl) 
        });

        return services;
    }
}