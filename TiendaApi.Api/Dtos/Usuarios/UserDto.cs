using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Dtos.Usuarios;

/// <summary>
/// DTO de usuario para respuestas de API.
/// Expone información pública del usuario sin datos sensibles como contraseñas.
///
/// <remarks>
/// Roles disponibles:
/// - "ADMIN": Acceso total al sistema
/// - "USER": Acceso limitado a recursos propios
/// </remarks>
/// </summary>
/// <example>
/// Respuesta JSON típica:
/// <code>
/// {
///   "id": 1,
///   "username": "juanperez",
///   "email": "juan@example.com",
///   "avatar": "https://ejemplo.com/avatars/juan.jpg",
///   "role": "USER",
///   "createdAt": "2024-01-01T00:00:00Z"
/// }
/// </code>
/// </example>
public record UserDto(
    /// <summary>
    /// Identificador único del usuario.
    /// Generado automáticamente por la base de datos.
    /// </summary>
    /// <example>1</example>
    long Id,

    /// <summary>
    /// Nombre de usuario único.
    /// Utilizado para autenticación junto con la contraseña.
    /// </summary>
    /// <example>juanperez</example>
    string Username,

    /// <summary>
    /// Correo electrónico del usuario.
    /// Direcciones únicas en el sistema.
    /// </summary>
    /// <example>juan@example.com</example>
    string Email,

    /// <summary>
    /// URL del avatar del usuario.
    /// Puede ser null si no ha configurado avatar.
    /// </summary>
    /// <example>https://ejemplo.com/avatars/juan.jpg</example>
    string Avatar,

    /// <summary>
    /// Rol del usuario en el sistema.
    /// Determina los permisos de acceso.
    /// </summary>
    /// <example>USER</example>
    string Role,

    /// <summary>
    /// Fecha y hora de creación de la cuenta en formato UTC.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    DateTime CreatedAt
);

/// <summary>
/// DTO para el registro de nuevos usuarios.
/// Define los datos necesarios para crear una nueva cuenta.
///
/// <remarks>
/// Validaciones de contraseña:
/// - Mínimo 6 caracteres
/// - Se recomienda incluir mayúsculas, minúsculas, números y símbolos
/// </remarks>
public record RegisterDto
{
    /// <summary>
    /// Nombre de usuario único.
    /// Identificador público del usuario en el sistema.
    /// </summary>
    /// <remarks>
    /// Restricciones:
    /// - Solo letras, números y guiones bajos
    /// - Sin espacios ni caracteres especiales
    /// - Debe ser único en el sistema
    /// </remarks>
    /// <example>juanperez</example>
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [MinLength(3, ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres")]
    [MaxLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Solo se permiten letras, números y guiones bajos")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario.
    /// Utilizado para notificaciones y recuperación de contraseña.
    /// </summary>
    /// <example>juan@example.com</example>
    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido")]
    [MaxLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// Almacenada de forma hasheada por seguridad.
    /// </summary>
    /// <example>Contraseña123!</example>
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO para el inicio de sesión de usuarios.
/// Credenciales necesarias para obtener token JWT.
public record LoginDto
{
    /// <summary>
    /// Nombre de usuario o correo electrónico.
    /// Identifica al usuario en el sistema.
    /// </summary>
    /// <example>juanperez</example>
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// Verificada contra el hash almacenado.
    /// </summary>
    /// <example>Contraseña123!</example>
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO de respuesta de autenticación con JWT.
/// Devuelto tras login o registro exitoso.
///
/// <remarks>
/// El token JWT debe enviarse en el header Authorization de solicitudes subsecuentes:
/// Authorization: Bearer &lt;token&gt;
/// </remarks>
public record AuthResponseDto(
    /// <summary>
    /// Token JWT para autenticación en requests.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    string Token,

    /// <summary>
    /// Información del usuario autenticado.
    /// </summary>
    UserDto User
);

/// <summary>
/// DTO para actualizar datos de usuario.
/// Campos opcionales: solo los presentes se actualizan.
public record UserUpdateDto
{
    /// <summary>
    /// Nuevo correo electrónico del usuario (opcional).
    /// Debe ser un email válido y único.
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
}
