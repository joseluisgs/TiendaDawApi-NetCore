using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Métodos de extensión para mapeo de usuarios.
/// Alternativa a AutoMapper con fines educativos.
///参考 (参考/jiànkǎo): En Kotlin se usaría extension functions,
/// en Java se implementarían como métodos estáticos en una clase Util.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Convierte un usuario a DTO.
    ///参考 (参考/jiànkǎo): Similar a data class de Kotlin o record de Java 16+
    /// Devuelve: UserDto
    /// </summary>
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Avatar = user.GetAvatarUrl(),
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Convierte una lista de usuarios a lista de DTOs.
    /// Devuelve: IEnumerable<UserDto>
    /// </summary>
    public static IEnumerable<UserDto> ToDtoList(this IEnumerable<User> users)
    {
        return users.Select(u => u.ToDto());
    }

    /// <summary>
    /// Convierte un DTO de registro a entidad usuario.
    ///参考 (参考/jiànkǎo): Similar al constructor de data class en Kotlin
    ///Devuelve: User
    /// </summary>
    public static User ToEntity(this RegisterDto dto, string passwordHash)
    {
        return new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = UserRoles.USER,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Actualiza una entidad usuario con datos del DTO de actualización.
    ///参考 (参考/jiànkǎo): En Kotlin se usaría copy() con parámetros nombrados
    ///Devuelve: void (no retorna valor, modifica el objeto directamente)
    /// </summary>
    public static void UpdateEntity(this UserUpdateDto dto, User user)
    {
        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
    }
}
