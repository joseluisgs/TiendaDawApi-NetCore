using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Dtos.Usuarios;

/// <summary>
/// DTO de usuario para respuestas de API (sin contraseña).
/// </summary>
public record UserDto(
    long Id,
    string Username,
    string Email,
    string Avatar,
    string Role,
    DateTime CreatedAt
);

/// <summary>
/// DTO para el registro de nuevos usuarios.
/// </summary>
public record RegisterDto
{
    /// <summary>
    /// Nombre de usuario único.
    /// </summary>
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [MinLength(3, ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres")]
    [MaxLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Solo se permiten letras, números y guiones bajos")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido")]
    [MaxLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO para el inicio de sesión de usuarios.
/// </summary>
public record LoginDto
{
    /// <summary>
    /// Nombre de usuario.
    /// </summary>
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO de respuesta de autenticación con JWT.
/// </summary>
public record AuthResponseDto(string Token, UserDto User);

/// <summary>
/// DTO para actualizar datos de usuario.
/// </summary>
public record UserUpdateDto
{
    /// <summary>
    /// Nuevo correo electrónico del usuario.
    /// </summary>
    [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido")]
    [MaxLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
    public string? Email { get; init; }

    /// <summary>
    /// Nueva contraseña del usuario.
    /// </summary>
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string? Password { get; init; }
}
