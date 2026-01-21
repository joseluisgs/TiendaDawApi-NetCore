using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
/// Implementación de IJwtTokenExtractor para extraer información de tokens JWT.
/// </summary>
public class JwtTokenExtractor : IJwtTokenExtractor
{
    private readonly ILogger<JwtTokenExtractor> _logger;

    public JwtTokenExtractor(ILogger<JwtTokenExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public long? ExtractUserId(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null) return null;

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.NameIdentifier || 
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == "nameid");

            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo userId del token");
            return null;
        }
    }

    /// <inheritdoc />
    public string? ExtractRole(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null) return null;

            var roleClaim = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == ClaimTypes.Role || 
                c.Type == "role");

            return roleClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo rol del token");
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsAdmin(string token)
    {
        var role = ExtractRole(token);
        return role?.Equals("admin", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <inheritdoc />
    public (long? UserId, bool IsAdmin, string? Role) ExtractUserInfo(string token)
    {
        var userId = ExtractUserId(token);
        var role = ExtractRole(token);
        var isAdmin = role?.Equals("admin", StringComparison.OrdinalIgnoreCase) ?? false;

        return (userId, isAdmin, role);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ExtractClaims(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            List<Claim> claims;

            if (jwtToken != null && jwtToken.Claims.Any())
            {
                claims = jwtToken.Claims.Select(c => new Claim(NormalizeClaimType(c.Type), c.Value, c.ValueType, c.Issuer, c.OriginalIssuer)).ToList();
            }
            else
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return null;

                var payload = parts[1];
                if (string.IsNullOrWhiteSpace(payload))
                    return null;

                var payloadJson = Base64UrlDecode(payload);
                var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
                using var doc = System.Text.Json.JsonDocument.Parse(payloadBytes);
                var root = doc.RootElement;

                claims = new List<Claim>();
                
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var name = prop.Name;
                        var value = prop.Value.GetString() ?? "";
                        var claimType = NormalizeClaimType(name);
                        
                        claims.Add(new Claim(claimType, value));
                    }
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo claims del token");
            return null;
        }
    }

    private static string NormalizeClaimType(string type)
    {
        var lower = type.ToLowerInvariant();
        return lower switch
        {
            "nameid" or "sub" => ClaimTypes.NameIdentifier,
            "email" => ClaimTypes.Email,
            "role" or "roles" => ClaimTypes.Role,
            "name" => ClaimTypes.Name,
            _ => type
        };
    }

    /// <inheritdoc />
    public string? ExtractEmail(string token)
    {
        try
        {
            var jwtToken = ReadToken(token);
            if (jwtToken == null) return null;

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => 
                c.Type == JwtRegisteredClaimNames.Email || 
                c.Type == ClaimTypes.Email);

            return emailClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extrayendo email del token");
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsValidTokenFormat(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            var header = parts[0];
            var payload = parts[1];
            var signature = parts[2];

            if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(payload))
                return false;

            if (!string.IsNullOrWhiteSpace(signature))
                return true;

            var headerJson = Base64UrlDecode(header);
            return headerJson.Contains("\"alg\":\"none\"") || headerJson.Contains("\"alg\" : \"none\"");
        }
        catch
        {
            return false;
        }
    }

    private static string Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private JwtSecurityToken? ReadToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogDebug("Token vacío o nulo");
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error al parsear token JWT");
            return null;
        }
    }
}
