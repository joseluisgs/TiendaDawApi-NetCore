using TiendaApi.Api.Models;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
/// Servicio para generación y validación de tokens JWT.
/// </summary>
public interface IJwtService
{
    /// <summary>Genera un token JWT para un usuario.</summary>
    /// <param name="user">Usuario para el token.</param>
    /// <returns>Token JWT generado.</returns>
    string GenerateToken(User user);

    /// <summary>Valida un token JWT.</summary>
    /// <param name="token">Token a validar.</param>
    /// <returns>Username del token o null si es inválido.</returns>
    string? ValidateToken(string token);
}
