using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Clase estática que proporciona métodos de extensión para el mapeo entre entidades de dominio y DTOs (Data Transfer Objects)
/// del modelo de Pedido.
///
/// <para><b>Patrón Mapper:</b></para>
/// Este mapper implementa el patrón de diseño "Mapper" o "Data Mapper", cuyo propósito es transferir datos entre
/// objetos en memoria y una base de datos, aislando la capa de dominio de los detalles de representación de datos.
///
/// <para><b>Por qué no se usa AutoMapper (con fines educativos):</b></para>
/// <list type="number">
///   <item>
///     <term>Comprensión profunda del mapeo</term>
///     <description>Al escribir los mapeos manualmente, los desarrolladores entienden exactamente cómo se transforman
///     los datos, qué campos se mapean y cuáles se ignoran. Esto es crucial para el aprendizaje.</description>
///   </item>
///   <item>
///     <term>Control total sobre la transformación</term>
///     <description>AutoMapper puede ocultar lógica de negocio importante. Al escribir mapeos explícitos, se hace
///     visible qué transformaciones se realizan (conversiones de tipos, formateo de fechas, cálculos).</description>
///   </item>
///   <item>
///     <term>性能 (Rendimiento)</term>
///     <description>AutoMapper usa reflexión y generación dinámica de IL, lo cual tiene overhead.
///     Los mapeos manuales son más eficientes en escenarios de alto rendimiento.</description>
///   </item>
///   <item>
///     <term>Flexibilidad para casos complejos</term>
///     <description>Cuando los mapeos no son simples copias de propiedades (flattening, renaming, condicionales,
///    计算 de campos derivados), AutoMapper puede resultar limitante o confuso.</description>
///   </item>
///   <item>
///     <term>Menor acoplamiento</term>
///     <description>No depender de una librería externa reduce las dependencias del proyecto y facilita el mantenimiento
///     a largo plazo.</description>
///   </item>
///   <item>
///     <term>Facilita las pruebas</term>
///     <description>Al ser métodos simples y explícitos, son más fáciles de probar y depurar.</description>
///   </item>
/// </list>
///
/// <para><b>Casos de uso apropiados para AutoMapper:</b></para>
/// En proyectos grandes con muchos mapeos simples y boilerplate repetitivo, AutoMapper puede acelerar el desarrollo.
/// Sin embargo, en esta API académica, se prioriza el aprendizaje de los fundamentos.
///
/// <para><b>Características especiales del PedidoMapper:</b></para>
/// Este mapper maneja objetos compuestos (Pedido con lista de PedidoItems) y calcula automáticamente
/// el subtotal de cada ítem y el total del pedido. También incluye el mapeo bidireccional entre
/// la entidad compuesta y sus DTOs correspondientes.
///
/// <para><b>Ejemplo de uso general:</b></para>
/// <code>
/// // Convertir entidad a DTO para respuesta API (incluye items calculados)
/// var pedidoDto = pedido.ToDto();
/// 
/// // Convertir lista de entidades a lista de DTOs
/// var pedidosDto = pedidos.ToDtoList();
/// 
/// // Crear entidad desde DTO de solicitud (con userId del contexto)
/// var pedido = dto.ToEntity(userId);
/// 
/// // Mapear ítem individual con datos adicionales del producto
/// var item = itemDto.ToEntity(nombreProducto, precio);
/// </code>
/// </summary>
public static class PedidoMapper
{
    /// <summary>
    /// Convierte una entidad de dominio <see cref="Pedido"/> a un DTO de respuesta <see cref="PedidoDto"/>
    /// para ser retornado en las respuestas de la API.
    /// </summary>
    /// <param name="pedido">La entidad de pedido a convertir.</param>
    /// <returns>Un nuevo objeto <see cref="PedidoDto"/> con los datos del pedido y sus ítems.</returns>
    /// <remarks>
    /// Este método mapea la entidad compuesta incluyendo todos los ítems del pedido y el destinatario.
    /// Convierte el Guid del ID a string para serialización JSON más amigable.
    /// Maneja gracefully el caso donde Items es null usando el operador null-coalescing.
    /// El estado del pedido se retorna como cadena, usando string.Empty si es null.
    /// El destinatario se mapea incluyendo su dirección (crea uno vacío si es null).
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint de API para ver detalles de un pedido
    /// [HttpGet("{id}")]
    /// public ActionResult&lt;PedidoDto&gt; GetPedido(Guid id)
    /// {
    ///     var pedido = _repo.GetById(id);
    ///     if () return NotFoundpedido == null();
    ///     return Ok(pedido.ToDto());
    /// }
    /// </code>
    /// </example>
    public static PedidoDto ToDto(this Pedido pedido)
    {
        return new PedidoDto(
            pedido.Id.ToString(),
            pedido.UserId,
            pedido.Destinatario?.ToDto() ?? new DestinatarioDto(),
            pedido.Items?.Select(i => i.ToDto()).ToList() ?? new(),
            pedido.Total,
            pedido.Estado ?? string.Empty,
            pedido.DireccionEnvio,
            pedido.CreatedAt
        );
    }

    /// <summary>
    /// Convierte una entidad de dominio <see cref="Destinatario"/> a un DTO <see cref="DestinatarioDto"/>
    /// para ser retornado en las respuestas de la API.
    /// </summary>
    /// <param name="destinatario">La entidad de destinatario a convertir.</param>
    /// <returns>Un nuevo objeto <see cref="DestinatarioDto"/> con los datos del destinatario.</returns>
    public static DestinatarioDto ToDto(this Destinatario? destinatario)
    {
        return new DestinatarioDto
        {
            NombreCompleto = destinatario?.NombreCompleto ?? string.Empty,
            Email = destinatario?.Email ?? string.Empty,
            Telefono = destinatario?.Telefono,
            Direccion = destinatario?.Direccion?.ToDto() ?? new DireccionDto()
        };
    }

    /// <summary>
    /// Convierte una entidad de dominio <see cref="Direccion"/> a un DTO <see cref="DireccionDto"/>
    /// para ser retornado en las respuestas de la API.
    /// </summary>
    /// <param name="direccion">La entidad de dirección a convertir.</param>
    /// <returns>Un nuevo objeto <see cref="DireccionDto"/> con los datos de la dirección.</returns>
    public static DireccionDto ToDto(this Direccion? direccion)
    {
        return new DireccionDto
        {
            Calle = direccion?.Calle ?? string.Empty,
            Numero = direccion?.Numero,
            Ciudad = direccion?.Ciudad ?? string.Empty,
            Provincia = direccion?.Provincia,
            Pais = direccion?.Pais ?? string.Empty,
            CodigoPostal = direccion?.CodigoPostal
        };
    }

    /// <summary>
    /// Convierte un DTO <see cref="DestinatarioDto"/> a una entidad de dominio <see cref="Destinatario"/>
    /// para ser persistida en la base de datos.
    /// </summary>
    /// <param name="dto">El DTO de destinatario a convertir.</param>
    /// <returns>Una nueva entidad <see cref="Destinatario"/> con los datos del DTO.</returns>
    public static Destinatario ToEntity(this DestinatarioDto dto)
    {
        return new Destinatario
        {
            NombreCompleto = dto.NombreCompleto,
            Email = dto.Email,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion?.ToEntity()
        };
    }

    /// <summary>
    /// Convierte un DTO <see cref="DireccionDto"/> a una entidad de dominio <see cref="Direccion"/>
    /// para ser persistida en la base de datos.
    /// </summary>
    /// <param name="dto">El DTO de dirección a convertir.</param>
    /// <returns>Una nueva entidad <see cref="Direccion"/> con los datos del DTO.</returns>
    public static Direccion? ToEntity(this DireccionDto? dto)
    {
        if (dto == null)
            return null;

        return new Direccion
        {
            Calle = dto.Calle,
            Numero = dto.Numero,
            Ciudad = dto.Ciudad,
            Provincia = dto.Provincia,
            Pais = dto.Pais,
            CodigoPostal = dto.CodigoPostal
        };
    }

    /// <summary>
    /// Convierte una colección de entidades de dominio <see cref="Pedido"/> a una colección de DTOs
    /// <see cref="PedidoDto"/> para ser retornados en las respuestas de la API.
    /// </summary>
    /// <param name="pedidos">La colección de entidades de pedido a convertir.</param>
    /// <returns>Una colección enumerable de objetos <see cref="PedidoDto"/>.</returns>
    /// <remarks>
    /// Utiliza LINQ Select internamente para transformar cada elemento.
    /// Devuelve un IEnumerable&lt;PedidoDto&gt; que se evalúa de forma diferida (lazy evaluation).
    /// Cada pedido incluirá sus respectivos ítems mapeados.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint para listar pedidos de un usuario
    /// [HttpGet("user/{userId}")]
    /// public ActionResult&lt;IEnumerable&lt;PedidoDto&gt;&gt; GetPedidosPorUsuario(long userId)
    /// {
    ///     var pedidos = _repo.GetByUserId(userId);
    ///     return Ok(pedidos.ToDtoList());
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<PedidoDto> ToDtoList(this IEnumerable<Pedido> pedidos)
    {
        return pedidos.Select(p => p.ToDto());
    }

    /// <summary>
    /// Convierte una entidad de dominio <see cref="PedidoItem"/> a un DTO de respuesta <see cref="PedidoItemDto"/>
    /// para ser incluido en la respuesta del pedido.
    /// </summary>
    /// <param name="item">La entidad de ítem de pedido a convertir.</param>
    /// <returns>Un nuevo objeto <see cref="PedidoItemDto"/> con los datos del ítem.</returns>
    /// <remarks>
    /// Calcula automáticamente el subtotal multiplicando precio por cantidad.
    /// Este método es idempotente y no modifica el objeto original.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Uso interno dentro de ToDto de Pedido
    /// var pedidoDto = new PedidoDto(
    ///     pedido.Id.ToString(),
    ///     pedido.UserId,
    ///     pedido.Items.Select(i => i.ToDto()).ToList(),
    ///     pedido.Total,
    ///     pedido.Estado,
    ///     pedido.DireccionEnvio,
    ///     pedido.CreatedAt
    /// );
    /// </code>
    /// </example>
    public static PedidoItemDto ToDto(this PedidoItem item)
    {
        return new PedidoItemDto(
            item.ProductoId,
            item.NombreProducto ?? string.Empty,
            item.Cantidad,
            item.Precio,
            item.Precio * item.Cantidad
        );
    }

    /// <summary>
    /// Convierte un DTO de solicitud <see cref="PedidoRequestDto"/> a una entidad de dominio <see cref="Pedido"/>
    /// para ser persistida en la base de datos.
    /// </summary>
    /// <param name="dto">El DTO de solicitud que contiene los datos del pedido proporcionados por el cliente.</param>
    /// <param name="userId">El ID del usuario que realiza el pedido (obtenido del contexto de autenticación).</param>
    /// <returns>Una nueva entidad <see cref="Pedido"/> con los datos del DTO.</returns>
    /// <remarks>
    /// Inicializa automáticamente las propiedades de auditoría CreatedAt y UpdatedAt con la fecha UTC actual.
    /// El ID del pedido se genera como Guid (UUID) automáticamente.
    /// El estado se establece como PENDIENTE por defecto (enum PedidoEstado).
    /// Los ítems del pedido se mapean usando el método ToEntity sobrecargado para PedidoItemRequestDto.
    /// El destinatario se mapea si está presente en el DTO.
    /// El total del pedido debe calcularse en el servicio o validarse contra los precios actuales.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint POST para crear un pedido
    /// [HttpPost]
    /// public ActionResult&lt;PedidoDto&gt; CreatePedido([FromBody] PedidoRequestDto dto)
    /// {
    ///     var userId = GetCurrentUserId(); // Del contexto de autenticación
    ///     var pedido = dto.ToEntity(userId);
    ///
    ///     // Validar disponibilidad de stock y calcular totales
    ///     _validator.ValidateAndCalculate(pedido);
    ///
    ///     _repo.Add(pedido);
    ///     _repo.SaveChanges();
    ///
    ///     return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedido.ToDto());
    /// }
    /// </code>
    /// </example>
    public static Pedido ToEntity(this PedidoRequestDto dto, long userId)
    {
        return new Pedido
        {
            UserId = userId,
            Destinatario = dto.Destinatario?.ToEntity(),
            Items = dto.Items.Select(i => i.ToEntity()).ToList(),
            Estado = PedidoEstado.PENDIENTE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Convierte un DTO de solicitud de ítem <see cref="PedidoItemRequestDto"/> a una entidad de dominio
    /// <see cref="PedidoItem"/> para ser incluida en un pedido.
    /// </summary>
    /// <param name="dto">El DTO de solicitud que contiene los datos del ítem.</param>
    /// <param name="nombreProducto">El nombre del producto en el momento de la compra (para histórico).</param>
    /// <param name="precio">El precio del producto en el momento de la compra (para histórico).</param>
    /// <returns>Una nueva entidad <see cref="PedidoItem"/> con los datos del DTO.</returns>
    /// <remarks>
    /// Este método permite enriquecer el ítem del pedido con información del producto al momento de la compra,
    /// creando un "snapshot" que preserva los datos incluso si el producto cambia después.
    /// Calcula automáticamente el subtotal como precio por cantidad.
    /// Los parámetros nombreProducto y precio son opcionales y default a string.Empty y 0 respectivamente.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un servicio al crear un pedido desde el carrito
    /// public Pedido CrearPedidoDesdeCarrito(CarritoDto carrito, long userId)
    /// {
    ///     var pedidoDto = new PedidoRequestDto
    ///     {
    ///         Items = carrito.Items.Select(item => {
    ///             var producto = _productRepo.GetById(item.ProductoId);
    ///             return new PedidoItemRequestDto
    ///             {
    ///                 ProductoId = item.ProductoId,
    ///                 Cantidad = item.Cantidad
    ///             }.ToEntity(producto.Nombre, producto.Precio);
    ///         }).ToList()
    ///     };
    ///     
    ///     return pedidoDto.ToEntity(userId);
    /// }
    /// </code>
    /// </example>
    public static PedidoItem ToEntity(this PedidoItemRequestDto dto, string? nombreProducto = null, decimal? precio = null)
    {
        return new PedidoItem
        {
            ProductoId = dto.ProductoId,
            NombreProducto = nombreProducto ?? string.Empty,
            Cantidad = dto.Cantidad,
            Precio = precio ?? 0,
            Subtotal = (precio ?? 0) * dto.Cantidad
        };
    }
}
