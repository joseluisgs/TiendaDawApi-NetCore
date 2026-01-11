using TiendaApi.Models;

namespace TiendaApi.Services.Auth;

/// <summary>
/// Servicio para generación y validación de tokens JWT.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera un token JWT para el usuario autenticado.
    /// </summary>
    string GenerateToken(User user);
    
    /// <summary>
    /// Valida un token JWT y retorna el username si es válido.
    /// </summary>
    string? ValidateToken(string token);
}
