using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de autenticación y autorización JWT.
/// </summary>
public static class AuthenticationConfig
{
    /// <summary>
    /// Configura autenticación JWT con tokens Bearer.
    /// </summary>
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("🔐 Configurando autenticación JWT...");

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TiendaApi";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TiendaApi";

        Log.Debug("🔑 JWT Issuer: {Issuer}", jwtIssuer);
        Log.Debug("🎯 JWT Audience: {Audience}", jwtAudience);

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

        Log.Information("🛡️ Configurando políticas de autorización...");
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole(UserRoles.ADMIN))
            .AddPolicy("RequireUserRole", policy => policy.RequireRole(UserRoles.USER, UserRoles.ADMIN))
            .AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.ADMIN));

        return services;
    }
}
