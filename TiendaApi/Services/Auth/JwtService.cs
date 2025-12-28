using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TiendaApi.Models.Entities;

namespace TiendaApi.Services.Auth;

/// <summary>
/// JWT Service implementation for token generation and validation
/// Java Spring Security equivalent: JwtTokenProvider implementation
/// Uses symmetric key from configuration (appsettings.json)
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generate JWT token with user claims
    /// Token includes: username, email, role, user ID
    /// </summary>
    public string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "TiendaApi";
        var audience = _configuration["Jwt:Audience"] ?? "TiendaApi";
        var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
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
        
        // Note: user.Username comes from database (already validated), not user input
        _logger.LogInformation("JWT token generated for user: {Username}", user.Username);
        
        return tokenString;
    }

    /// <summary>
    /// Validate JWT token and extract username
    /// Returns null if token is invalid
    /// </summary>
    public string? ValidateToken(string token)
    {
        try
        {
            var key = _configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Key not configured");
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
            var username = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

            return username;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            return null;
        }
    }
}
