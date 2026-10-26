using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Puertas de login externo (OAuth2/OIDC), activables por configuración.
///
/// Cada puerta solo se registra si Auth:{Proveedor}:Enabled es true: con la
/// puerta cerrada la app ni siquiera conoce al proveedor. Los ClientId y
/// ClientSecret van en user-secrets o variables de entorno, nunca en appsettings.json:
///   dotnet user-secrets set "Google:ClientId" "xxxx.apps.googleusercontent.com"
///   dotnet user-secrets set "Google:ClientSecret" "GOCSPX-xxxx"
///
/// La puerta principal (JWT Bearer) ya está registrada en AuthenticationConfig.
/// Aquí solo se AÑADEN esquemas de proveedores encima: el default sigue siendo JWT.
/// </summary>
public static class OAuthConfig
{
    /// <summary>
    /// Registra las puertas externas activadas en la configuración.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app (sección Auth).</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddOAuthDoors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Un único AddAuthentication() como menú de puertas: los esquemas
        // defaults (JWT Bearer) ya los puso AuthenticationConfig, aquí solo se
        // añaden esquemas de proveedores encima.
        var auth = services.AddAuthentication();

        if (configuration.GetValue<bool>("Auth:Google:Enabled"))
        {
            auth.AddGoogle(options =>
            {
                options.ClientId = configuration["Google:ClientId"]
                    ?? throw new InvalidOperationException(
                        "Google:ClientId no configurado (user-secrets o env var)");
                options.ClientSecret = configuration["Google:ClientSecret"]
                    ?? throw new InvalidOperationException(
                        "Google:ClientSecret no configurado (user-secrets o env var)");
                options.UsePkce = true;
            });
            Log.Information("🚪 Puerta de login externo activa: Google");
        }

        if (configuration.GetValue<bool>("Auth:GitHub:Enabled"))
        {
            auth.AddGitHub(options =>
            {
                options.ClientId = configuration["GitHub:ClientId"]
                    ?? throw new InvalidOperationException(
                        "GitHub:ClientId no configurado (user-secrets o env var)");
                options.ClientSecret = configuration["GitHub:ClientSecret"]
                    ?? throw new InvalidOperationException(
                        "GitHub:ClientSecret no configurado (user-secrets o env var)");
            });
            Log.Information("🚪 Puerta de login externo activa: GitHub");
        }

        if (configuration.GetValue<bool>("Auth:Microsoft:Enabled"))
        {
            // AddMicrosoftAccount viene en caja del SDK: no necesita paquete NuGet extra
            auth.AddMicrosoftAccount(options =>
            {
                options.ClientId = configuration["Microsoft:ClientId"]
                    ?? throw new InvalidOperationException(
                        "Microsoft:ClientId no configurado (user-secrets o env var)");
                options.ClientSecret = configuration["Microsoft:ClientSecret"]
                    ?? throw new InvalidOperationException(
                        "Microsoft:ClientSecret no configurado (user-secrets o env var)");
            });
            Log.Information("🚪 Puerta de login externo activa: Microsoft");
        }

        return services;
    }
}
