namespace TiendaApi.Apis.Models;

/// <summary>
/// Entidad de usuario en la base de datos.
/// </summary>
public class User
{
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Fecha de última actualización del usuario.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Constantes para los roles de usuario.
/// </summary>
public static class UserRoles
{
    public const string ADMIN = "ADMIN";
    public const string USER = "USER";
}
