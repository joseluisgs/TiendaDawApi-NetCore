using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
///     Implementación de IJwtTokenExtractor para extraer información de tokens JWT.
/// </summary>
public class JwtTokenExtractor : IJwtTokenExtractor {
    private readonly ILogger<JwtTokenExtractor> _logger;

    /// <summary>
    ///     Crea una instancia del extractor de tokens JWT.
    /// </summary>
    /// <param name="logger">Logger de la instancia.</param>
    public JwtTokenExtractor(ILogger<JwtTokenExtractor> logger) {
        _logger = logger;
    }

    /// <inheritdoc />
    public long? ExtractUserId(string token) {
        try {
            JwtSecurityToken? jwtToken = ReadToken(token);
            if (jwtToken == null) {
                return null;
            }

            Claim? userIdClaim = jwtToken.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == "nameid");

            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId)) {
                return userId;
            }

            return null;
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error extrayendo userId del token");
            return null;
        }
    }

    /// <inheritdoc />
    public string? ExtractRole(string token) {
        try {
            JwtSecurityToken? jwtToken = ReadToken(token);
            if (jwtToken == null) {
                return null;
            }

            Claim? roleClaim = jwtToken.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Role ||
                c.Type == "role");

            return roleClaim?.Value;
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error extrayendo rol del token");
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsAdmin(string token) {
        string? role = ExtractRole(token);
        return role?.Equals("admin", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <inheritdoc />
    public (long? UserId, bool IsAdmin, string? Role) ExtractUserInfo(string token) {
        long? userId = ExtractUserId(token);
        string? role = ExtractRole(token);
        bool isAdmin = role?.Equals("admin", StringComparison.OrdinalIgnoreCase) ?? false;

        return (userId, isAdmin, role);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ExtractClaims(string token) {
        try {
            JwtSecurityToken? jwtToken = ReadToken(token);
            List<Claim> claims;

            if (jwtToken != null && jwtToken.Claims.Any()) {
                claims = jwtToken.Claims.Select(c =>
                    new Claim(NormalizeClaimType(c.Type), c.Value, c.ValueType, c.Issuer, c.OriginalIssuer)).ToList();
            } else {
                string[] parts = token.Split('.');
                if (parts.Length != 3) {
                    return null;
                }

                string payload = parts[1];
                if (string.IsNullOrWhiteSpace(payload)) {
                    return null;
                }

                string payloadJson = Base64UrlDecode(payload);
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
                using var doc = JsonDocument.Parse(payloadBytes);
                JsonElement root = doc.RootElement;

                claims = new List<Claim>();

                foreach (JsonProperty prop in root.EnumerateObject()) {
                    if (prop.Value.ValueKind == JsonValueKind.String) {
                        string name = prop.Name;
                        string value = prop.Value.GetString() ?? "";
                        string claimType = NormalizeClaimType(name);

                        claims.Add(new Claim(claimType, value));
                    }
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error extrayendo claims del token");
            return null;
        }
    }

    /// <inheritdoc />
    public string? ExtractEmail(string token) {
        try {
            JwtSecurityToken? jwtToken = ReadToken(token);
            if (jwtToken == null) {
                return null;
            }

            Claim? emailClaim = jwtToken.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Email ||
                c.Type == ClaimTypes.Email);

            return emailClaim?.Value;
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error extrayendo email del token");
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsValidTokenFormat(string token) {
        if (string.IsNullOrWhiteSpace(token)) {
            return false;
        }

        try {
            string[] parts = token.Split('.');
            if (parts.Length != 3) {
                return false;
            }

            string header = parts[0];
            string payload = parts[1];
            string signature = parts[2];

            if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(payload)) {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(signature)) {
                return true;
            }

            string headerJson = Base64UrlDecode(header);
            return headerJson.Contains("\"alg\":\"none\"") || headerJson.Contains("\"alg\" : \"none\"");
        } catch {
            return false;
        }
    }

    private static string NormalizeClaimType(string type) {
        string lower = type.ToLowerInvariant();
        return lower switch {
            "nameid" or "sub" => ClaimTypes.NameIdentifier,
            "email" => ClaimTypes.Email,
            "role" or "roles" => ClaimTypes.Role,
            "name" => ClaimTypes.Name,
            _ => type
        };
    }

    private static string Base64UrlDecode(string input) {
        string base64 = input.Replace('-', '+').Replace('_', '/');

        switch (base64.Length % 4) {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        byte[] bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private JwtSecurityToken? ReadToken(string token) {
        if (string.IsNullOrWhiteSpace(token)) {
            _logger.LogDebug("Token vacío o nulo");
            return null;
        }

        try {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        } catch (Exception ex) {
            _logger.LogDebug(ex, "Error al parsear token JWT");
            return null;
        }
    }
}
