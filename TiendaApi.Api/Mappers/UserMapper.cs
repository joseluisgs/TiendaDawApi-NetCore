using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Mapper para convertir entre User y sus DTOs.
/// </summary>
public static class UserMapper
{
    /// <summary>Convierte User a UserDto.</summary>
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Username, user.Email, user.GetAvatarUrl(), user.Role, user.CreatedAt);

    /// <summary>Convierte lista de Users a lista de UserDto.</summary>
    public static IEnumerable<UserDto> ToDtoList(this IEnumerable<User> users) =>
        users.Select(u => u.ToDto());

    /// <summary>Convierte RegisterDto a User.</summary>
    public static User ToEntity(this RegisterDto dto, string passwordHash) => new()
    {
        Username = dto.Username,
        Email = dto.Email,
        PasswordHash = passwordHash,
        Role = UserRoles.USER,
        IsDeleted = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>Actualiza User desde UserUpdateDto.</summary>
    public static void UpdateEntity(this UserUpdateDto dto, User user)
    {
        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
    }

    /// <summary>Actualiza User desde UserPatchDto (actualización parcial).</summary>
    public static void UpdateEntity(this UserPatchDto dto, User user)
    {
        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
        if (dto.Avatar != null)
            user.Avatar = dto.Avatar;
    }
}
