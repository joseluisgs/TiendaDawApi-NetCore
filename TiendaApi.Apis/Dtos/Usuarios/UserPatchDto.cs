using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Dtos.Usuarios;

/// <summary>
/// DTO para actualización parcial de usuario (PATCH).
/// </summary>
public record UserPatchDto
{
    /// <summary>
    /// Nuevo correo electrónico del usuario (opcional).
    /// </summary>
    [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido")]
    [MaxLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
    public string? Email { get; init; }

    /// <summary>
    /// Nueva contraseña del usuario (opcional).
    /// </summary>
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string? Password { get; init; }

    /// <summary>
    /// Nueva URL del avatar del usuario (opcional).
    /// </summary>
    [MaxLength(500, ErrorMessage = "La URL del avatar no puede exceder 500 caracteres")]
    [Url(ErrorMessage = "Debe ser una URL válida")]
    public string? Avatar { get; init; }
}
