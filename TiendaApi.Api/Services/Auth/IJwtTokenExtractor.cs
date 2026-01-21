using System.Security.Claims;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
/// Interfaz para extraer información de tokens JWT.
/// </summary>
public interface IJwtTokenExtractor
{
    /// <summary>Extrae el ID del usuario del token.</summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>ID del usuario o null si es inválido.</returns>
    long? ExtractUserId(string token);

    /// <summary>Extrae el rol del usuario del token.</summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>Rol del usuario o null.</returns>
    string? ExtractRole(string token);

    /// <summary>Determina si el usuario es admin.</summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>True si el rol es "admin".</returns>
    bool IsAdmin(string token);

    /// <summary>Extrae información completa del usuario.</summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>Tupla con (userId, isAdmin, role).</returns>
    (long? UserId, bool IsAdmin, string? Role) ExtractUserInfo(string token);

    /// <summary>Extrae todos los claims del token.</summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>ClaimsPrincipal o null si es inválido.</returns>
    ClaimsPrincipal? ExtractClaims(string token);

    /// <summary>Extrae el email del usuario del token.</summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>Email del usuario o null.</returns>
    string? ExtractEmail(string token);

    /// <summary>Valida el formato del token (no verifica expiración).</summary>
    /// <param name="token">Token JWT.</param>
    /// <returns>True si el formato es válido.</returns>
    bool IsValidTokenFormat(string token);
}
