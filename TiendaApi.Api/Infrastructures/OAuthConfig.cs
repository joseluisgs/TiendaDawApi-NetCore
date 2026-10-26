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
            var clientId = configuration["Google:ClientId"];
            var clientSecret = configuration["Google:ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Log.Warning("🚪 Auth:Google:Enabled=true pero falta ClientId/ClientSecret — puerta Google NO registrada");
            }
            else
            {
                auth.AddGoogle(options =>
                {
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.UsePkce = true;
                });
                Log.Information("🚪 Puerta de login externo activa: Google");
            }
        }

        if (configuration.GetValue<bool>("Auth:GitHub:Enabled"))
        {
            var clientId = configuration["GitHub:ClientId"];
            var clientSecret = configuration["GitHub:ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Log.Warning("🚪 Auth:GitHub:Enabled=true pero falta ClientId/ClientSecret — puerta GitHub NO registrada");
            }
            else
            {
                auth.AddGitHub(options =>
                {
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                });
                Log.Information("🚪 Puerta de login externo activa: GitHub");
            }
        }

        if (configuration.GetValue<bool>("Auth:Microsoft:Enabled"))
        {
            var clientId = configuration["Microsoft:ClientId"];
            var clientSecret = configuration["Microsoft:ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Log.Warning("🚪 Auth:Microsoft:Enabled=true pero falta ClientId/ClientSecret — puerta Microsoft NO registrada");
            }
            else
            {
                auth.AddMicrosoftAccount(options =>
                {
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                });
                Log.Information("🚪 Puerta de login externo activa: Microsoft");
            }
        }

        return services;
    }
}
