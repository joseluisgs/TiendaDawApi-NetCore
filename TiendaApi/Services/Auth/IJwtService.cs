using TiendaApi.Models.Entities;

namespace TiendaApi.Services.Auth;

/// <summary>
/// Service for JWT token generation and validation
/// Java Spring Security equivalent: JwtTokenProvider
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT token for authenticated user
    /// </summary>
    string GenerateToken(User user);
    
    /// <summary>
    /// Validate JWT token and return username if valid
    /// </summary>
    string? ValidateToken(string token);
}
