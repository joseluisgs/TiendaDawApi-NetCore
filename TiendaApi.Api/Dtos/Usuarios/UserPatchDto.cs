using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Dtos.Usuarios;

/// <summary>
/// DTO para actualización parcial de usuario (PATCH).
/// Permite modificar campos específicos sin enviar todos los datos.
///
/// <remarks>
/// Uso típico:
/// - PATCH /api/users/{id}
/// - Actualizar perfil de usuario
/// - Cambiar avatar
/// </remarks>
public record UserPatchDto
{
    /// <summary>
    /// Nuevo correo electrónico del usuario (opcional).
    /// Debe ser un email válido y único en el sistema.
    /// </summary>
    /// <example>nuevo@example.com</example>
    [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido")]
    [MaxLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
    public string? Email { get; init; }

    /// <summary>
    /// Nueva contraseña del usuario (opcional).
    /// Mínimo 6 caracteres.
    /// </summary>
    /// <example>NuevaContraseña456!</example>
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string? Password { get; init; }

    /// <summary>
    /// Nueva URL del avatar del usuario (opcional).
    /// Enlace a la imagen de perfil del usuario.
    /// </summary>
    /// <example>https://ejemplo.com/avatars/nuevo-avatar.jpg</example>
    [MaxLength(500, ErrorMessage = "La URL del avatar no puede exceder 500 caracteres")]
    [Url(ErrorMessage = "Debe ser una URL válida")]
    public string? Avatar { get; init; }
}
