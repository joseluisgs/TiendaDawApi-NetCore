using FluentValidation;
using TiendaApi.Apis.Dtos.Usuarios;

namespace TiendaApi.Apis.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para RegisterDto.
/// Implementa el patrón FluentValidation para definir reglas de validación de forma declarativa y legible.
/// Este validador se integra con el contenedor de inyección de dependencias y se utiliza en los endpoints
/// de la API para validar las solicitudes de registro de nuevos usuarios.
/// </summary>
/// <remarks>
/// <para><b>Patrón FluentValidation:</b></para>
/// <para>Este validador hereda de AbstractValidator&lt;RegisterDto&gt; y define las reglas de validación
/// en el constructor mediante el método RuleFor(). Este enfoque permite:</para>
/// <list type="number">
///   <item><description>Encadenar múltiples reglas de validación para cada propiedad</description></item>
///   <item><description>Personalizar mensajes de error con WithMessage()</description></item>
///   <item><description>Aplicar expresiones regulares con Matches() para formatos específicos</description></item>
///   <item><description>Validar formatos de email con EmailAddress()</description></item>
/// </list>
/// <para><b>Integración con servicios:</b></para>
/// <para>Los validadores se registran en el contenedor de servicios (Program.cs) mediante:
/// services.AddValidatorsFromAssemblyContaining&lt;RegisterValidator&gt;();
/// Esto permite que FluentValidation inyecte automáticamente IValidator&lt;RegisterDto&gt;
/// en los constructores de los controladores o servicios que lo requieran.</para>
/// <para><b>Flujo de validación:</b></para>
/// <para>Cuando se recibe una solicitud HTTP al endpoint de registro, el filtro de validación
/// automáticamente invoca el validador, retorna errores 400 Bad Request si la validación
/// falla, y permite continuar al controlador si pasa. La verificación de usuario existente
/// se realiza en el servicio de negocio, no aquí.</para>
/// </remarks>
/// <example>
/// <b>Petición válida:</b>
/// <code>
/// {
///   "username": "juan_perez",
///   "email": "juan.perez@correo.com",
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
///     "Username": [
///       "El nombre de usuario es obligatorio",
///       "Solo se permiten letras, números y guiones bajos"
///     ],
///     "Email": ["Debe ser un correo electrónico válido"],
///     "Password": ["La contraseña debe tener al menos 6 caracteres"]
///   }
/// }
/// </code>
/// </example>
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    /// <summary>
    /// Constructor que define las reglas de validación para RegisterDto.
    /// </summary>
    /// <remarks>
    /// <para><b>Regla: Username con formato válido</b></para>
    /// <para>La propiedad Username debe cumplir cuatro condiciones:</para>
    /// <list type="number">
    ///   <item><description>NotEmpty: El username no puede ser nulo, vacío o solo espacios</description></item>
    ///   <item><description>MinimumLength: Debe tener al menos 3 caracteres de longitud</description></item>
    ///   <item><description>MaximumLength: No puede exceder 50 caracteres</description></item>
    ///   <item><description>Matches: Solo permite letras (a-z, A-Z), números (0-9) y guiones bajos (_)</description></item>
    /// </list>
    /// <para>La expresión regular ^[a-zA-Z0-9_]+$ asegura un formato seguro para identificadores.</para>
    /// </remarks>
    /// <example>
    /// <b>Error cuando Username tiene caracteres especiales:</b>
    /// "errors": { "Username": ["Solo se permiten letras, números y guiones bajos"] }
    /// <b>Error cuando Username es muy corto:</b>
    /// "errors": { "Username": ["El nombre de usuario debe tener al menos 3 caracteres"] }
    /// </example>
    public RegisterValidator()
    {
        RuleFor(r => r.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .MinimumLength(3).WithMessage("El nombre de usuario debe tener al menos 3 caracteres")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede exceder 50 caracteres")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Solo se permiten letras, números y guiones bajos");

        /// <summary>
        /// Regla: Email válido.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: Email con formato válido</b></para>
        /// <para>La propiedad Email debe cumplir tres condiciones:</para>
        /// <list type="number">
        ///   <item><description>NotEmpty: El email es obligatorio</description></item>
        ///   <item><description>EmailAddress: Valida el formato estándar de email</description></item>
        ///   <item><description>MaximumLength: No puede exceder 100 caracteres</description></item>
        /// </list>
        /// <para>FluentValidation.EmailAddress usa una validación que verifica la estructura básica
        /// del email (local-part@domain). La verificación de dominio real se hace en el servicio.</para>
        /// </remarks>
        /// <example>
        /// <b>Error cuando Email no tiene formato válido:</b>
        /// "errors": { "Email": ["Debe ser un correo electrónico válido"] }
        /// </example>
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio")
            .EmailAddress().WithMessage("Debe ser un correo electrónico válido")
            .MaximumLength(100).WithMessage("El correo no puede exceder 100 caracteres");

        /// <summary>
        /// Regla: Password con longitud válida.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: Password obligatoria y con longitud válida</b></para>
        /// <para>La propiedad Password debe cumplir tres condiciones:</para>
        /// <list type="number">
        ///   <item><description>NotEmpty: La contraseña es obligatoria</description></item>
        ///   <item><description>MinimumLength: Mínimo 6 caracteres para seguridad básica</description></item>
        ///   <item><description>MaximumLength: No puede exceder 100 caracteres</description></item>
        /// </list>
        /// <para>Nota: Este validador solo verifica longitud. La complejidad de contraseña
        /// (mayúsculas, números, símbolos) se valida en el servicio de negocio o con un
        /// validador adicional personalizado.</para>
        /// </remarks>
        /// <example>
        /// <b>Error cuando Password está vacía:</b>
        /// "errors": { "Password": ["La contraseña es obligatoria"] }
        /// <b>Error cuando Password es muy corta:</b>
        /// "errors": { "Password": ["La contraseña debe tener al menos 6 caracteres"] }
        /// </example>
        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres");
    }
}
