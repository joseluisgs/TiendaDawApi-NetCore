namespace TiendaApi.Models.Entities;

/// <summary>
/// Entity representing a user with authentication
/// Stored in PostgreSQL using Entity Framework Core
/// 
/// Java Spring Security equivalent: UserDetails implementation
/// Passwords should be hashed (BCrypt recommended)
/// </summary>
public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.USER; // ADMIN or USER
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Constants for user roles
/// Java equivalent: enum or constants for @PreAuthorize
/// </summary>
public static class UserRoles
{
    public const string ADMIN = "ADMIN";
    public const string USER = "USER";
}
