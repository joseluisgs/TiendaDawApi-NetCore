using FluentValidation;
using TiendaApi.Apis.Dtos.Pedidos;

namespace TiendaApi.Apis.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para PedidoRequestDto.
/// Implementa el patrón FluentValidation para definir reglas de validación de forma declarativa y legible.
/// Este validador se integra con el contenedor de inyección de dependencias y se utiliza en los endpoints
/// de la API para validar las solicitudes de creación de pedidos.
/// </summary>
/// <remarks>
/// <para><b>Patrón FluentValidation:</b></para>
/// <para>Este validador hereda de AbstractValidator&lt;PedidoRequestDto&gt; y define las reglas de validación
/// en el constructor mediante el método RuleFor(). Este enfoque permite:</para>
/// <list type="number">
///   <item><description>Validar estructuras complejas como listas de items</description></item>
///   <item><description>Personalizar mensajes de error con WithMessage()</description></item>
///   <item><description>Aplicar reglas condicionales con Must() para lógica personalizada</description></item>
///   <item><description>Validar que las colecciones no estén vacías</description></item>
/// </list>
/// <para><b>Integración con servicios:</b></para>
/// <para>Los validadores se registran en el contenedor de servicios (Program.cs) mediante:
/// services.AddValidatorsFromAssemblyContaining&lt;PedidoRequestValidator&gt;();
/// Esto permite que FluentValidation inyecte automáticamente IValidator&lt;PedidoRequestDto&gt;
/// en los constructores de los controladores o servicios que lo requieran.</para>
/// <para><b>Flujo de validación:</b></para>
/// <para>Cuando se recibe una solicitud de creación de pedido, el filtro de validación
/// automáticamente invoca el validador. La validación principal verifica que existan
/// items en el pedido. La validación de cada item individual (precio, cantidad, producto)
/// se hace en un validador anidado (ItemPedidoValidator) o en el servicio de negocio.</para>
/// </remarks>
/// <example>
/// <b>Petición válida:</b>
/// <code>
/// {
///   "items": [
///     { "productoId": 1, "cantidad": 2 },
///     { "productoId": 5, "cantidad": 1 }
///   ]
/// }
/// </code>
/// <b>Petición inválida (respuesta 400) - Sin items:</b>
/// <code>
/// {
///   "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
///   "title": "One or more validation errors occurred.",
///   "status": 400,
///   "errors": {
///     "Items": [
///       "El pedido debe contener artículos",
///       "El pedido debe contener al menos un artículo"
///     ]
///   }
/// }
/// </code>
/// <b>Petición inválida (respuesta 400) - Items null:</b>
/// <code>
/// {
///   "errors": {
///     "Items": ["El pedido debe contener artículos"]
///   }
/// }
/// </code>
/// </example>
public class PedidoRequestValidator : AbstractValidator<PedidoRequestDto>
{
    /// <summary>
    /// Constructor que define las reglas de validación para PedidoRequestDto.
    /// </summary>
    /// <remarks>
    /// <para><b>Regla: Items del pedido obligatorios y no vacíos</b></para>
    /// <para>La propiedad Items es una lista de PedidoItemDto que representa los productos
    /// solicitados. Se aplican tres validaciones redundantes para asegurar robustness:</para>
    /// <list type="number">
    ///   <item><description>NotNull: La lista no puede ser null (evita NullReferenceException)</description></item>
    ///   <item><description>NotEmpty: La lista debe contener al menos un elemento</description></item>
    ///   <item><description>Must: Validación personalizada que verifica Count >= 1 (maneja null safety)</description></item>
    /// </list>
    /// <para>La redundancia en las reglas es intencional para proporcionar mensajes de error
    /// más específicos según el caso de falla (null vs empty).</para>
    /// <para>Nota: La validación de cada item individual (productoId válido, cantidad > 0,
    /// stock disponible) se realiza en un validador anidado o en el servicio de negocio.</para>
    /// </remarks>
    /// <example>
    /// <b>Error cuando Items es null:</b>
    /// "errors": { "Items": ["El pedido debe contener artículos"] }
    /// <b>Error cuando Items está vacío []:</b>
    /// "errors": { "Items": ["El pedido debe contener al menos un artículo"] }
    /// </example>
    public PedidoRequestValidator()
    {
        RuleFor(p => p.Items)
            .NotNull().WithMessage("El pedido debe contener artículos")
            .NotEmpty().WithMessage("El pedido debe contener al menos un artículo")
            .Must(items => items == null || items.Count >= 1).WithMessage("El pedido debe contener al menos un artículo");
    }
}
