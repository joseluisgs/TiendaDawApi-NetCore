using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Transforma entidades de usuario en DTOs de seguridad.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Crea un <see cref="UserDto"/> desde un <see cref="User"/>.
    /// </summary>
    public static UserDto ToDto(this User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.Avatar,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };

    /// <summary>
    /// Crea una respuesta completa de autenticación.
    /// </summary>
    public static AuthResponseDto ToAuthResponse(this User user, string token) =>
        new()
        {
            Token = token,
            User = user.ToDto()
        };
}