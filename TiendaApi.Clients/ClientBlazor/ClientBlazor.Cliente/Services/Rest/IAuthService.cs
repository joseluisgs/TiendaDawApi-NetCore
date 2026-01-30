using CSharpFunctionalExtensions;
using ClientBlazor.Cliente.DTOs.Auth;
using ClientBlazor.Cliente.Domain.Errors;

namespace ClientBlazor.Cliente.Services.Rest;

/// <summary>
/// Define el contrato para el servicio de orquestación de autenticación.
/// Maneja la lógica de negocio relacionada con la sesión del usuario y su persistencia.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Realiza el proceso de inicio de sesión contra la API.
    /// Valida credenciales, actualiza el estado global y persiste la sesión localmente.
    /// </summary>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="password">Contraseña secreta.</param>
    /// <returns>Un resultado con los datos de respuesta o un error de dominio.</returns>
    Task<Result<AuthResponseDto, DomainError>> LoginAsync(string email, string password);

    /// <summary>
    /// Cierra la sesión activa del usuario.
    /// Limpia el estado reactivo y elimina los datos del almacenamiento local.
    /// </summary>
    Task LogoutAsync();
}