namespace TiendaApi.Api.Models;

using TiendaApi.Api.Data.Abstractions;

/// <summary>
/// Entidad de dominio que representa un usuario.
/// Autenticación con email/password hasheado con BCrypt. Roles: USER, ADMIN.
/// </summary>
public class User : ITimestamped
{
    /// <summary>URL de avatar por defecto cuando no hay imagen.</summary>
    public const string AVATAR_DEFAULT = "https://via.placeholder.com/150";

    /// <summary>Prefijo para avatares locales (/storage/images/usuarios/).</summary>
    public const string AVATAR_LOCAL_PREFIX = "/storage/images/usuarios/";

    /// <summary>ID único del usuario (PK en PostgreSQL).</summary>
    public long Id { get; set; }

    /// <summary>Nombre de usuario público (3-50 caracteres, único).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email del usuario (obligatorio, único).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash BCrypt de la contraseña (60 caracteres aprox).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>URL o ruta del avatar (null = usa AVATAR_DEFAULT).</summary>
    public string? Avatar { get; set; }

    /// <summary>Rol del usuario (USER o ADMIN).</summary>
    public string Role { get; set; } = UserRoles.USER;

    /// <summary>Indica si el usuario está eliminado (soft-delete).</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Determina si el avatar es local (almacenado en el servidor).
    /// </summary>
    /// <returns>true si Avatar comienza con "/storage".</returns>
    public bool IsLocalAvatar() => !string.IsNullOrEmpty(Avatar) && Avatar.StartsWith("/storage", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determina si el usuario usa el avatar por defecto.
    /// </summary>
    /// <returns>true si Avatar es null, vacío o igual a AVATAR_DEFAULT.</returns>
    public bool HasDefaultAvatar() => string.IsNullOrEmpty(Avatar) || Avatar == AVATAR_DEFAULT;

    /// <summary>
    /// Obtiene la URL completa del avatar normalizada.
    /// </summary>
    /// <returns>URL lista para usar en &lt;img src&gt;.</returns>
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
    /// <summary>Rol de administrador con acceso total al sistema.</summary>
    public const string ADMIN = "ADMIN";

    /// <summary>Rol de usuario estándar con permisos básicos.</summary>
    public const string USER = "USER";
}
