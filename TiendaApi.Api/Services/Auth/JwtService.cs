using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
/// Implementación de IJwtService para generación y validación de tokens JWT.
/// </summary>
public class JwtService(
    IConfiguration configuration,
    ILogger<JwtService> logger
) : IJwtService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<JwtService> _logger = logger;

    /// <summary>
    /// Genera un token JWT firmado con la información del usuario.
    /// </summary>
    /// <param name="user">Usuario para el token.</param>
    /// <returns>Token JWT firmado.</returns>
    /// <exception cref="InvalidOperationException">Si la clave JWT no está configurada.</exception>
    public string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada");
        var issuer = _configuration["Jwt:Issuer"] ?? "TiendaApi";
        var audience = _configuration["Jwt:Audience"] ?? "TiendaApi";
        var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        _logger.LogInformation("Token JWT generado para usuario: {Username}", user.Username);
        
        return tokenString;
    }

    /// <summary>
    /// Valida un token JWT y extrae el nombre de usuario.
    /// </summary>
    /// <param name="token">Token JWT a validar.</param>
    /// <returns>Username del token o null si es inválido.</returns>
    public string? ValidateToken(string token)
    {
        try
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key no configurada");
            var issuer = _configuration["Jwt:Issuer"] ?? "TiendaApi";
            var audience = _configuration["Jwt:Audience"] ?? "TiendaApi";

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var username = jwtToken.Claims.First(x => x.Type == "username").Value;

            return username;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validación de token JWT fallida");
            return null;
        }
    }
}
