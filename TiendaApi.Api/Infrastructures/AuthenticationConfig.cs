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

        // 🎓 Mecanismo OFICIAL de Microsoft para APIs sin UI ("Identity without
        // UI"): AddIdentityCore registra UserManager/validadores/lockout SIN
        // registrar el esquema de cookies Identity.Application. Con AddIdentity
        // (la variante "con todo"), la cookie se convierte en DefaultScheme y
        // los [Authorize(Roles=...)] (que construyen su política sin esquemas)
        // iban al handler de cookies → redirect /Account/Login (401) en todo
        // endpoint de admin. AddRoles + AddSignInManager añaden lo necesario
        // para roles y CheckPasswordSignInAsync sin tocar el DefaultScheme.
        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            // 🎓 Mínimo de 8 (OWASP: >= 8); con 6 una contraseña corta
            // de solo letras y dígitos era suficientemente barata.
            options.Password.RequiredLength = 8;
            // 🎓 false EXPLÍCITO: el default de Identity es true y dejarlo implícito
            // hacía que contraseñas fijas de tests/E2E sin símbolos (p.ej. "User1234")
            // fueran válidas en Custom pero rechazadas en Identity. La política se
            // declara entera una vez aquí y los validadores DTO la reflejan.
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<long>>()
        .AddSignInManager()
        .AddEntityFrameworkStores<TiendaApi.Api.Data.TiendaDbContext>()
        .AddDefaultTokenProviders();

        // 🎓 Defensa en profundidad: aunque AddIdentityCore no ensucia el
        // DefaultScheme, lo fijamos explícitamente a Bearer para que ningún
        // registro futuro (u otra librería) pueda cambiar el comportamiento.
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
        // Aunque el modo Identity usa AddIdentityCore (que NO ensucia el
        // DefaultScheme con la cookie Identity.Application), declarar el esquema
        // aquí inmuniza las políticas ante cualquier cambio futuro: si alguien
        // registrara AddIdentity (con cookie) u otra librería tocara el
        // DefaultScheme, los [Authorize(Roles=...)] seguirían autenticando con
        // JWT en vez de redirigir a /Account/Login. Es la buena práctica en
        // cualquier API JWT: la política es autosuficiente.
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
