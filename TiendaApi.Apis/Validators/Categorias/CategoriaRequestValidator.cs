using FluentValidation;
using TiendaApi.Apis.Dtos.Categorias;

namespace TiendaApi.Apis.Validators.Categorias;

/// <summary>
/// Validador FluentValidation para CategoriaRequestDto.
/// Implementa el patrón FluentValidation para definir reglas de validación de forma declarativa y legible.
/// Este validador se integra con el contenedor de inyección de dependencias y se utiliza en los endpoints
/// de la API para validar las solicitudes de creación y actualización de categorías.
/// </summary>
/// <remarks>
/// <para><b>Patrón FluentValidation:</b></para>
/// <para>Este validador hereda de AbstractValidator&lt;CategoriaRequestDto&gt; y define las reglas de validación
/// en el constructor mediante el método RuleFor(). Este enfoque permite:</para>
/// <list type="number">
///   <item><description>Encadenar múltiples reglas de validación para cada propiedad</description></item>
///   <item><description>Personalizar mensajes de error con WithMessage()</description></item>
///   <item><description>Aplicar reglas condicionales con When()</description></item>
///   <item><description>Reutilizar validadores compuestos con SetValidator()</description></item>
/// </list>
/// <para><b>Integración con servicios:</b></para>
/// <para>Los validadores se registran en el contenedor de servicios (Program.cs) mediante:
/// services.AddValidatorsFromAssemblyContaining&lt;CategoriaRequestValidator&gt;();
/// Esto permite que FluentValidation inyecte automáticamente IValidator&lt;CategoriaRequestDto&gt;
/// en los constructores de los controladores o servicios que lo requieran.</para>
/// <para><b>Flujo de validación:</b></para>
/// <para>Cuando se recibe una solicitud HTTP, el filtro de validación (FluentValidation.AspNetCore)
/// automáticamente invoca el validador correspondiente, retorna errores 400 Bad Request
/// si la validación falla, y permite continuar al controlador si pasa.</para>
/// </remarks>
/// <example>
/// <b>Petición válida:</b>
/// <code>
/// {
///   "nombre": "Electrónica"
/// }
/// </code>
/// <b>Petición inválida (respuesta 400):</b>
/// <code>
/// {
///   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
///   "title": "One or more validation errors occurred.",
///   "status": 400,
///   "errors": {
///     "Nombre": [
///       "El nombre es obligatorio",
///       "El nombre debe tener al menos 3 caracteres"
///     ]
///   }
/// }
/// </code>
/// </example>
public class CategoriaRequestValidator : AbstractValidator<CategoriaRequestDto>
{
    /// <summary>
    /// Constructor que define las reglas de validación para CategoriaRequestDto.
    /// </summary>
    /// <remarks>
    /// <para><b>Regla: Nombre obligatorio y con longitud válida</b></para>
    /// <para>La propiedad Nombre debe cumplir tres condiciones:</para>
    /// <list type="number">
    ///   <item><description>NotEmpty: El nombre no puede ser nulo, vacío o solo espacios en blanco</description></item>
    ///   <item><description>MinimumLength: Debe tener al menos 3 caracteres de longitud</description></item>
    ///   <item><description>MaximumLength: No puede exceder 100 caracteres</description></item>
    /// </list>
    /// <para>Estas reglas se evalúan en orden y todos los mensajes de error fallidos se incluyen en la respuesta.</para>
    /// </remarks>
    /// <example>
    /// <b>Error cuando Nombre está vacío:</b>
    /// "errors": { "Nombre": ["El nombre es obligatorio"] }
    /// <b>Error cuando Nombre es muy corto:</b>
    /// "errors": { "Nombre": ["El nombre debe tener al menos 3 caracteres"] }
    /// <b>Error cuando Nombre es muy largo:</b>
    /// "errors": { "Nombre": ["El nombre no puede exceder 100 caracteres"] }
    /// </example>
    public CategoriaRequestValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");
    }
}
