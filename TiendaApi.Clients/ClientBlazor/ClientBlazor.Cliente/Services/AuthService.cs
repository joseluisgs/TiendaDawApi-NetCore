using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.State;
using ClientBlazor.Cliente.DTOs.Auth;
using CSharpFunctionalExtensions;

namespace ClientBlazor.Cliente.Services;

/// <summary>
/// Servicio de autenticación.
/// Maneja login, validación y errores del dominio.
/// </summary>
public class AuthService(
    /// <summary>
    /// Store de autenticación para gestionar el estado global.
    /// </summary>
    AuthStore authStore,
    /// <summary>
    /// Store de notificaciones para mostrar mensajes al usuario.
    /// </summary>
    NotificationStore notificationStore)
{

    /// <summary>
    /// Realiza el proceso completo de login.
    /// Devuelve Result<AuthResponseDto, DomainError> para que el componente maneje el resultado.
    /// </summary>
    /// <param name="email">Email del usuario.</param>
    /// <param name="password">Contraseña del usuario.</param>
    /// <returns>Resultado del login con AuthResponseDto o DomainError.</returns>
    public async Task<Result<AuthResponseDto, DomainError>> LoginAsync(string email, string password)
    {
        try
        {
            // Validar credenciales
            ValidateCredentials(email, password);

            // Simular llamada a API
            await Task.Delay(500);

            // Generar token ficticio
            var token = GenerateFakeToken(email, email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "ADMIN" : "USER");

            // Crear usuario (simulado)
            var user = new UserDto(
                Id: 1,
                Username: email.Split('@')[0],
                Email: email,
                Avatar: $"/avatars/{email.Split('@')[0]}.jpg",
                Role: email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "ADMIN" : "USER",
                CreatedAt: DateTime.UtcNow.AddDays(-30)
            );

            var authResponse = new AuthResponseDto(token, user);

            // Actualizar estado de autenticación
            authStore.SetAuth(
                token: token,
                email: user.Email,
                nombre: user.Username,
                role: user.Role
            );

            // Notificar éxito
            notificationStore.Success($"Bienvenido, {user.Username}! Has iniciado sesión correctamente.", "Login Exitoso");

            return Result.Success<AuthResponseDto, DomainError>(authResponse);
        }
        catch (DomainError domainError)
        {
            return Result.Failure<AuthResponseDto, DomainError>(domainError);
        }
        catch (Exception)
        {
            return Result.Failure<AuthResponseDto, DomainError>(GeneralErrors.Unexpected);
        }
    }

    /// <summary>
    /// Valida las credenciales de login.
    /// Lanza DomainError si hay problemas.
    /// </summary>
    /// <param name="email">Email a validar.</param>
    /// <param name="password">Contraseña a validar.</param>
    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw ValidationErrors.EmptyField("email");

        if (string.IsNullOrWhiteSpace(password))
            throw ValidationErrors.EmptyField("password");

        if (!email.Contains('@'))
            throw ValidationErrors.InvalidEmail;
    }

    /// <summary>
    /// Información del usuario autenticado.
    /// Incluye datos del usuario y token JWT.
    /// </summary>
    /// <param name="Email">Correo electrónico del usuario.</param>
    /// <param name="Nombre">Nombre completo del usuario.</param>
    /// <param name="Role">Rol del usuario en el sistema.</param>
    /// <param name="Token">Token JWT para autenticación.</param>
    public record UserInfo(string Email, string Nombre, string Role, string Token)
    {
        /// <summary>
        /// Nombre para mostrar (nombre o parte del email).
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(Nombre) ? Email.Split('@')[0] : Nombre;
    }

    /// <summary>
    /// Genera un token ficticio para simular autenticación JWT.
    /// Crea un token con estructura JWT pero con firma falsa.
    /// </summary>
    /// <param name="email">Email del usuario para incluir en el payload.</param>
    /// <param name="role">Rol del usuario para incluir en el payload.</param>
    /// <returns>Token JWT ficticio con estructura válida.</returns>
    private static string GenerateFakeToken(string email, string role)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{email}:{role}:{timestamp}";
        var fakeSignature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload)).Substring(0, 32);

        return $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"sub\":\"{email}\",\"role\":\"{role}\",\"iat\":{timestamp}}}"))}.{fakeSignature}";
    }
}