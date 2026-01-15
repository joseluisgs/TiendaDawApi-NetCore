using FluentValidation;
using TiendaApi.Apis.Dtos.Productos;

namespace TiendaApi.Apis.Validators.Productos;

/// <summary>
/// Validador FluentValidation para ProductoRequestDto.
/// Implementa el patrón FluentValidation para definir reglas de validación de forma declarativa y legible.
/// Este validador se integra con el contenedor de inyección de dependencias y se utiliza en los endpoints
/// de la API para validar las solicitudes de creación y actualización de productos.
/// </summary>
/// <remarks>
/// <para><b>Patrón FluentValidation:</b></para>
/// <para>Este validador hereda de AbstractValidator&lt;ProductoRequestDto&gt; y define las reglas de validación
/// en el constructor mediante el método RuleFor(). Este enfoque permite:</para>
/// <list type="number">
///   <item><description>Encadenar múltiples reglas de validación para cada propiedad</description></item>
///   <item><description>Personalizar mensajes de error con WithMessage()</description></item>
///   <item><description>Aplicar reglas condicionales con When()</description></item>
///   <item><description>Validar formatos complejos con Must() para lógica personalizada</description></item>
/// </list>
/// <para><b>Integración con servicios:</b></para>
/// <para>Los validadores se registran en el contenedor de servicios (Program.cs) mediante:
/// services.AddValidatorsFromAssemblyContaining&lt;ProductoRequestValidator&gt;();
/// Esto permite que FluentValidation inyecte automáticamente IValidator&lt;ProductoRequestDto&gt;
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
///   "nombre": "iPhone 15 Pro",
///   "descripcion": "Último modelo de Apple con chip A17 Pro",
///   "precio": 999.99,
///   "stock": 50,
///   "imagen": "https://ejemplo.com/iphone15.jpg",
///   "categoriaId": 1
/// }
/// </code>
/// <b>Petición inválida (respuesta 400):</b>
/// <code>
/// {
///   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
///   "title": "One or more validation errors occurred.",
///   "status": 400,
///   "errors": {
///     "Nombre": ["El nombre es obligatorio"],
///     "Precio": ["El precio debe ser mayor a 0"],
///     "CategoriaId": ["Debe seleccionar una categoría válida"]
///   }
/// }
/// </code>
/// </example>
public class ProductoRequestValidator : AbstractValidator<ProductoRequestDto>
{
    /// <summary>
    /// Constructor que define las reglas de validación para ProductoRequestDto.
    /// </summary>
    /// <remarks>
    /// <para><b>Regla: Nombre obligatorio y con longitud válida</b></para>
    /// <para>La propiedad Nombre debe cumplir tres condiciones:</para>
    /// <list type="number">
    ///   <item><description>NotEmpty: El nombre no puede ser nulo, vacío o solo espacios en blanco</description></item>
    ///   <item><description>MinimumLength: Debe tener al menos 3 caracteres de longitud</description></item>
    ///   <item><description>MaximumLength: No puede exceder 200 caracteres</description></item>
    /// </list>
    /// <para>Estas reglas se evalúan secuencialmente y se acumulan todos los mensajes de error.</para>
    /// </remarks>
    /// <example>
    /// <b>Error cuando Nombre está vacío:</b>
    /// "errors": { "Nombre": ["El nombre es obligatorio"] }
    /// </example>
    public ProductoRequestValidator()
    {
        RuleFor(p => p.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        /// <summary>
        /// Regla: Descripción con longitud máxima opcional.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: Descripción con longitud máxima</b></para>
        /// <para>La propiedad Descripcion tiene una restricción de longitud máxima de 1000 caracteres,
        /// pero es opcional (puede ser null o vacía). Se aplica la regla solo cuando la descripción
        /// no está vacía mediante el método When().</para>
        /// <list type="number">
        ///   <item><description>MaximumLength: Limita a 1000 caracteres máximo</description></item>
        ///   <item><description>When(): Solo aplica la regla si Descripcion no es null ni empty</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <b>Error cuando Descripcion excede 1000 caracteres:</b>
        /// "errors": { "Descripcion": ["La descripción no puede exceder 1000 caracteres"] }
        /// </example>
        RuleFor(p => p.Descripcion)
            .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres")
            .When(p => !string.IsNullOrEmpty(p.Descripcion));

        /// <summary>
        /// Regla: Precio mayor a cero.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: Precio positivo</b></para>
        /// <para>La propiedad Precio debe ser estrictamente mayor a cero para evitar productos
        /// gratuitos o con precio inválido. No permite valores negativos ni cero.</para>
        /// <list type="number">
        ///   <item><description>GreaterThan(0): El precio debe ser mayor a 0</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <b>Error cuando Precio es 0:</b>
        /// "errors": { "Precio": ["El precio debe ser mayor a 0"] }
        /// <b>Error cuando Precio es negativo:</b>
        /// "errors": { "Precio": ["El precio debe ser mayor a 0"] }
        /// </example>
        RuleFor(p => p.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0");

        /// <summary>
        /// Regla: Stock no negativo.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: Stock no negativo</b></para>
        /// <para>La propiedad Stock puede ser cero (producto agotado) pero nunca negativo.
        /// Permite valores enteros mayores o iguales a cero.</para>
        /// <list type="number">
        ///   <item><description>GreaterThanOrEqualTo(0): El stock puede ser 0 o mayor</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <b>Error cuando Stock es negativo:</b>
        /// "errors": { "Stock": ["El stock no puede ser negativo"] }
        /// </example>
        RuleFor(p => p.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo");

        /// <summary>
        /// Regla: URL de imagen válida y con longitud máxima.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: URL de imagen válida</b></para>
        /// <para>La propiedad Imagen es opcional pero si se proporciona debe ser una URL válida
        /// con esquema http o https. Se valida mediante Must() con lógica personalizada.</para>
        /// <list type="number">
        ///   <item><description>MaximumLength: Limita a 500 caracteres máximo</description></item>
        ///   <item><description>Must(): Valida que sea una URL absoluta con esquema http o https</description></item>
        ///   <item><description>When(): Solo aplica si la imagen no está vacía</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <b>Error cuando URL excede 500 caracteres:</b>
        /// "errors": { "Imagen": ["La URL de la imagen no puede exceder 500 caracteres"] }
        /// <b>Error cuando URL no es válida:</b>
        /// "errors": { "Imagen": ["Debe ser una URL válida (http:// o https://)"] }
        /// </example>
        RuleFor(p => p.Imagen)
            .MaximumLength(500).WithMessage("La URL de la imagen no puede exceder 500 caracteres")
            .Must(url => string.IsNullOrEmpty(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("Debe ser una URL válida (http:// o https://)")
            .When(p => !string.IsNullOrEmpty(p.Imagen));

        /// <summary>
        /// Regla: Categoría válida.
        /// </summary>
        /// <remarks>
        /// <para><b>Regla: CategoriaId válido</b></para>
        /// <para>La propiedad CategoriaId debe ser mayor a cero, indicando que debe existir
        /// una categoría relacionada en la base de datos. La existencia real se valida
        /// en el servicio de negocio, esta es solo una validación de formato.</para>
        /// <list type="number">
        ///   <item><description>GreaterThan(0): El ID de categoría debe ser positivo</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <b>Error cuando CategoriaId es 0 o negativo:</b>
        /// "errors": { "CategoriaId": ["Debe seleccionar una categoría válida"] }
        /// </example>
        RuleFor(p => p.CategoriaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");
    }
}
