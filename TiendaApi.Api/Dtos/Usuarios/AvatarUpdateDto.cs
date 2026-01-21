using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Dtos.Usuarios;

/// <summary>
/// DTO para actualizar el avatar de un usuario.
/// </summary>
/// <remarks>
/// Uso típico:
/// - PATCH /api/usuarios/{id}/avatar
/// - Actualizar imagen de perfil del usuario
/// </remarks>
public record AvatarUpdateDto
{
    /// <summary>
    /// URL del nuevo avatar.
    /// </summary>
    /// <value>URL absoluta (http/https) o ruta local (/storage/).</value>
    /// <example>https://example.com/avatar.jpg</example>
    [MaxLength(500, ErrorMessage = "La URL del avatar no puede exceder 500 caracteres")]
    [Url(ErrorMessage = "Debe ser una URL válida")]
    public string AvatarUrl { get; init; } = string.Empty;
}
