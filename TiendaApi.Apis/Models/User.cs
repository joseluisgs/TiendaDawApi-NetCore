namespace TiendaApi.Apis.Models;

using TiendaApi.Apis.Data;

/// <summary>
/// Entidad de usuario en la base de datos.
/// </summary>
public class User : ITimestamped
{
    public const string AVATAR_DEFAULT = "https://via.placeholder.com/150";
    public const string AVATAR_LOCAL_PREFIX = "/storage/images/usuarios/";

    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Nombre de usuario.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// Hash de la contraseña del usuario.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>
    /// URL del avatar del usuario.
    /// </summary>
    public string? Avatar { get; set; }
    /// <summary>
    /// Rol del usuario (ADMIN o USER).
    /// </summary>
    public string Role { get; set; } = UserRoles.USER;
    /// <summary>
    /// Indica si el usuario está eliminado.
    /// </summary>
    public bool IsDeleted { get; set; }
    /// <summary>
    /// Fecha de creación del usuario.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    /// <summary>
    /// Fecha de última actualización del usuario.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Verifica si el avatar es local (almacenado en el servidor).
    /// </summary>
    public bool IsLocalAvatar() => !string.IsNullOrEmpty(Avatar) && Avatar.StartsWith("/storage", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Verifica si el avatar es el por defecto.
    /// </summary>
    public bool HasDefaultAvatar() => string.IsNullOrEmpty(Avatar) || Avatar == AVATAR_DEFAULT;

    /// <summary>
    /// Obtiene la URL completa del avatar para mostrar.
    /// </summary>
    public string GetAvatarUrl()
    {
        if (string.IsNullOrEmpty(Avatar))
            return AVATAR_DEFAULT;

        if (Avatar.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            Avatar.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Avatar;

        if (Avatar.StartsWith("/storage", StringComparison.OrdinalIgnoreCase))
            return Avatar;

        if (Avatar.StartsWith("/"))
            return $"/storage{Avatar}";

        return $"{AVATAR_LOCAL_PREFIX}{Avatar}";
    }
}

/// <summary>
/// Constantes para los roles de usuario.
/// </summary>
public static class UserRoles
{
    public const string ADMIN = "ADMIN";
    public const string USER = "USER";
}
