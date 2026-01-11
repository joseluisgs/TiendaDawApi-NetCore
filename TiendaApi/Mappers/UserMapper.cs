using TiendaApi.Dtos.Usuarios;
using TiendaApi.Models;

namespace TiendaApi.Mappers;

/// <summary>
/// Extension methods for User entity-DTO conversions
/// Alternative to AutoMapper for educational purposes
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Converts User entity to UserDto
    /// </summary>
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Converts IEnumerable<User> to IEnumerable<UserDto>
    /// </summary>
    public static IEnumerable<UserDto> ToDtoList(this IEnumerable<User> users)
    {
        return users.Select(u => u.ToDto());
    }

    /// <summary>
    /// Converts RegisterDto to User entity (for signup)
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
    /// Updates an existing User entity with data from UserUpdateDto
    /// </summary>
    public static void UpdateEntity(this UserUpdateDto dto, User user)
    {
        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
        user.UpdatedAt = DateTime.UtcNow;
    }
}
