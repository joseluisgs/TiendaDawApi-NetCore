using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de autenticación y autorización.
///
/// Soporta dos modos según Auth:Mode en configuración:
/// - "Custom" (default): BCrypt manual + JWT Bearer (enfoque propio)
/// - "Identity": ASP.NET Core Identity (UserManager/SignInManager) + JWT Bearer
///
/// En ambos modos el resultado es el mismo: un JWT Bearer que los endpoints validan.
/// La diferencia es CÓMO se genera y valida la contraseña internamente.
/// </summary>
public static class AuthenticationConfig
{
    /// <summary>
    /// Configura autenticación según el modo seleccionado.
    /// </summary>
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authMode = configuration["Auth:Mode"] ?? "Custom";
        Log.Information("🔐 Configurando autenticación... Modo: {Mode}", authMode);

        if (authMode == "Identity")
        {
            return AddIdentityAuthentication(services, configuration);
        }

        return AddCustomAuthentication(services, configuration);
    }

    /// <summary>
    /// Modo Custom: BCrypt manual + JWT Bearer (el enfoque original).
    /// </summary>
    private static IServiceCollection AddCustomAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("🔐 Modo Custom: BCrypt + JWT Bearer");

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TiendaApi";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TiendaApi";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        AddAuthorizationPolicies(services);

        // Auth: Custom (BCrypt manual)
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    /// <summary>
    /// Modo Identity: ASP.NET Core Identity + JWT Bearer.
    /// Identity maneja passwords (PBKDF2), lockout, user store, etc.
    /// </summary>
    private static IServiceCollection AddIdentityAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("🔐 Modo Identity: ASP.NET Core Identity + JWT Bearer");

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TiendaApi";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TiendaApi";

        services.AddIdentity<User, IdentityRole<long>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<TiendaApi.Api.Data.TiendaDbContext>()
        .AddDefaultTokenProviders();

        // 🎓 AddIdentity deja Identity.Application (cookie) como DefaultScheme.
        // Los atributos [Authorize(Roles=...)] construyen su política SIN esquemas
        // y usan el DefaultScheme: sin este fijado a Bearer, esas peticiones iban
        // al handler de cookies → redirect a /Account/Login (401) en modo Identity.
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        AddAuthorizationPolicies(services);

        // Auth: Identity (UserManager/SignInManager)
        services.AddScoped<IAuthService, IdentityAuthService>();

        return services;
    }

    /// <summary>
    /// Políticas de autorización (comunes a ambos modos).
    /// </summary>
    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        Log.Information("🛡️ Configurando políticas de autorización...");
        // 🎓 Todas las políticas declaran el esquema Bearer EXPLÍCITAMENTE.
        // Sin esquema, la política usa el DefaultScheme de autenticación: en
        // modo Identity, AddIdentity registra la cookie Identity.Application como
        // DefaultScheme y el middleware redirigía a /Account/Login (401) en todos
        // los endpoints protegidos con política nombrada (RequireAdminRole...).
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy("RequireAdminRole", new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(UserRoles.ADMIN)
                .Build())
            .AddPolicy("RequireUserRole", new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(UserRoles.USER, UserRoles.ADMIN)
                .Build())
            .AddPolicy("AdminOnly", new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(UserRoles.ADMIN)
                .Build());
    }
}
