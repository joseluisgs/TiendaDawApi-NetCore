using FluentValidation;
using TiendaApi.Apis.Dtos.Usuarios;

namespace TiendaApi.Apis.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para LoginDto.
/// Implementa el patrón FluentValidation para definir reglas de validación de forma declarativa y legible.
/// Este validador se integra con el contenedor de inyección de dependencias y se utiliza en los endpoints
/// de la API para validar las solicitudes de inicio de sesión de usuarios.
/// </summary>
/// <remarks>
/// <para><b>Patrón FluentValidation:</b></para>
/// <para>Este validador hereda de AbstractValidator&lt;LoginDto&gt; y define las reglas de validación
/// en el constructor mediante el método RuleFor(). Este enfoque permite:</para>
/// <list type="number">
///   <item><description>Definir reglas mínimas para las credenciales de login</description></item>
///   <item><description>Personalizar mensajes de error con WithMessage()</description></item>
///   <item><description>Validar solo la presencia de datos, no su validez</description></item>
/// </list>
/// <para><b>Integración con servicios:</b></para>
/// <para>Los validadores se registran en el contenedor de servicios (Program.cs) mediante:
/// services.AddValidatorsFromAssemblyContaining&lt;LoginValidator&gt;();
/// Esto permite que FluentValidation inyecte automáticamente IValidator&lt;LoginDto&gt;
/// en los constructores de los controladores o servicios que lo requieran.</para>
/// <para><b>Flujo de validación:</b></para>
/// <para>El validador de Login es intencionalmente mínimo. Solo verifica que el username
/// y password no estén vacíos. La autenticación real (verificar credenciales, generar JWT)
/// se realiza en el servicio de negocio. Esto permite dar mensajes de error genéricos
/// para evitar revelar información sobre usuarios existentes.</para>
/// </remarks>
/// <example>
/// <b>Petición válida:</b>
/// <code>
/// {
///   "username": "juan_perez",
///   "password": "SecurePass123"
/// }
/// </code>
/// <b>Petición inválida (respuesta 400):</b>
/// <code>
/// {
///   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
///   "title": "One or more validation errors occurred.",
///   "status": 400,
///   "errors": {
///     "Username": ["El nombre de usuario es obligatorio"],
///     "Password": ["La contraseña es obligatoria"]
///   }
/// }
/// </code>
/// <b>Nota: Si las credenciales son inválidas (pero el formato es correcto), la API retornará
/// 401 Unauthorized desde el servicio de negocio, no desde el validador.</b>
/// </example>
public class LoginValidator : AbstractValidator<LoginDto>
{
    /// <summary>
    /// Constructor que define las reglas de validación para LoginDto.
    /// </summary>
    /// <remarks>
    /// <para><b>Regla: Username obligatorio</b></para>
    /// <para>La propiedad Username no puede ser nula, vacía o solo espacios en blanco.
    /// Se usa NotEmpty() que verifica que la cadena tenga al menos un carácter no espacio.</para>
    /// <list type="number">
    ///   <item><description>NotEmpty: El username es obligatorio para el login</description></item>
    /// </list>
    /// <para>No se valida el formato exacto del username aquí para evitar revelar información
    /// sobre qué formatos son válidos o qué usuarios existen en el sistema.</para>
    /// </remarks>
    /// <example>
    /// <b>Error cuando Username está vacío:</b>
    /// "errors": { "Username": ["El nombre de usuario es obligatorio"] }
    /// </example>
    public LoginValidator()
    {
        RuleFor(l => l.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio");

        /// <summary>
        /// Regla: Password obligatorio.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: Password obligatorio</b></para>
        /// <para>La propiedad Password no puede ser nula, vacía o solo espacios en blanco.
        /// Se usa NotEmpty() que verifica que la cadena tenga al menos un carácter no espacio.</para>
        /// <list type="number">
        ///   <item><description>NotEmpty: La contraseña es obligatoria para el login</description></item>
        /// </list>
        /// <para>No se valida la longitud mínima aquí para mantener el validador simple.
        /// La verificación de credenciales correctas se hace en el servicio de negocio.</para>
        /// </remarks>
        /// <example>
        /// <b>Error cuando Password está vacía:</b>
        /// "errors": { "Password": ["La contraseña es obligatoria"] }
        /// </example>
        RuleFor(l => l.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria");
    }
}
