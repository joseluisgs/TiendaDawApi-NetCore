using CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Auth;

/// <summary>
/// Servicio de autenticación usando Patrón Result.
/// Encapsula la lógica de autenticación con Programación Orientada al Resultado.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registrar un nuevo usuario.
    /// Flujo: Validar → Verificar duplicados → Hashear password → Guardar → Generar token.
    /// </summary>
    Task<Result<AuthResponseDto, DomainError>> SignUpAsync(RegisterDto dto);

    /// <summary>
    /// Autenticar un usuario existente.
    /// Flujo: Validar → Buscar usuario → Verificar password → Generar token.
    /// </summary>
    Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto dto);
}
