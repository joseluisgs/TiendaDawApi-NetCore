using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
/// Contrato del servicio de autenticación.
/// </summary>
public interface IAuthService
{
    /// <summary>Registra un nuevo usuario.</summary>
    /// <param name="dto">Datos de registro.</param>
    /// <returns>Resultado con respuesta de autenticación.</returns>
    Task<Result<AuthResponseDto, DomainError>> SignUpAsync(RegisterDto dto);

    /// <summary>Inicia sesión con credenciales.</summary>
    /// <param name="dto">Credenciales de acceso.</param>
    /// <returns>Resultado con respuesta de autenticación.</returns>
    Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto dto);
}
