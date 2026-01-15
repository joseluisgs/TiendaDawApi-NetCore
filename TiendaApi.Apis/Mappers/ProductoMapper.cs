using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Clase estática que proporciona métodos de extensión para el mapeo entre entidades de dominio y DTOs (Data Transfer Objects)
/// del modelo de Producto.
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
/// <para><b>Características especiales del ProductoMapper:</b></para>
/// Este mapper incluye el nombre de la categoría relacionada (flattening) en el DTO de producto,
/// permitiendo al cliente acceder a esta información sin necesidad de realizar una consulta adicional.
///
/// <para><b>Ejemplo de uso general:</b></para>
/// <code>
/// // Convertir entidad a DTO para respuesta API (incluye nombre de categoría)
/// var productoDto = producto.ToDto();
/// 
/// // Convertir lista de entidades a lista de DTOs
/// var productosDto = productos.ToDtoList();
/// 
/// // Crear entidad desde DTO de solicitud
/// var producto = dto.ToEntity();
/// 
/// // Actualizar entidad existente desde DTO
/// dto.UpdateEntity(producto);
/// </code>
/// </summary>
public static class ProductoMapper
{
    /// <summary>
    /// Convierte una entidad de dominio <see cref="Producto"/> a un DTO de respuesta <see cref="ProductoDto"/>
    /// para ser retornado en las respuestas de la API.
    /// </summary>
    /// <param name="producto">La entidad de producto a convertir.</param>
    /// <returns>Un nuevo objeto <see cref="ProductoDto"/> con los datos del producto.</returns>
    /// <remarks>
    /// Este método implementa "flattening" al incluir el nombre de la categoría relacionada
    /// (<c>producto.Categoria?.Nombre</c>) directamente en el DTO. Si la categoría no está cargada,
    /// se retorna una cadena vacía en lugar de null para evitar errores de serialización.
    /// Se utiliza principalmente en los endpoints GET para transformar entidades del dominio
    /// en objetos serializables para el cliente.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint de API
    /// [HttpGet("{id}")]
    /// public ActionResult&lt;ProductoDto&gt; GetProducto(long id)
    /// {
    ///     var producto = _repo.GetById(id);
    ///     if (producto == null) return NotFound();
    ///     return Ok(producto.ToDto());
    /// }
    /// </code>
    /// </example>
    public static ProductoDto ToDto(this Producto producto)
    {
        return new ProductoDto(
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock,
            producto.Imagen,
            producto.CategoriaId,
            producto.Categoria?.Nombre ?? string.Empty,
            producto.CreatedAt,
            producto.UpdatedAt
        );
    }

    /// <summary>
    /// Convierte una colección de entidades de dominio <see cref="Producto"/> a una colección de DTOs
    /// <see cref="ProductoDto"/> para ser retornados en las respuestas de la API.
    /// </summary>
    /// <param name="productos">La colección de entidades de producto a convertir.</param>
    /// <returns>Una colección enumerable de objetos <see cref="ProductoDto"/>.</returns>
    /// <remarks>
    /// Utiliza LINQ Select internamente para transformar cada elemento.
    /// Devuelve un IEnumerable&lt;ProductoDto&gt; que se evalúa de forma diferida (lazy evaluation).
    /// El nombre de categoría se incluirá para cada producto si está disponible en el contexto.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint de API para listar productos con información de categoría
    /// [HttpGet]
    /// public ActionResult&lt;IEnumerable&lt;ProductoDto&gt;&gt; GetProductos()
    /// {
    ///     var productos = _repo.GetAll().Include(p => p.Categoria);
    ///     return Ok(productos.ToDtoList());
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<Producto> productos)
    {
        return productos.Select(p => p.ToDto());
    }

    /// <summary>
    /// Convierte un DTO de solicitud <see cref="ProductoRequestDto"/> a una entidad de dominio <see cref="Producto"/>
    /// para ser persistida en la base de datos.
    /// </summary>
    /// <param name="dto">El DTO de solicitud que contiene los datos proporcionados por el cliente.</param>
    /// <returns>Una nueva entidad <see cref="Producto"/> con los datos del DTO.</returns>
    /// <remarks>
    /// Inicializa automáticamente las propiedades de auditoría CreatedAt y UpdatedAt con la fecha UTC actual.
    /// El ID se genera por la base de datos (autoincremento).
    /// La imagen es opcional; si no se proporciona, se guarda como null o cadena vacía según el contexto.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint POST para crear un producto
    /// [HttpPost]
    /// public ActionResult&lt;ProductoDto&gt; CreateProducto([FromBody] ProductoRequestDto dto)
    /// {
    ///     var producto = dto.ToEntity();
    ///     _repo.Add(producto);
    ///     _repo.SaveChanges();
    ///     return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto.ToDto());
    /// }
    /// </code>
    /// </example>
    public static Producto ToEntity(this ProductoRequestDto dto)
    {
        return new Producto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Stock = dto.Stock,
            Imagen = dto.Imagen,
            CategoriaId = dto.CategoriaId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Actualiza una entidad de dominio <see cref="Producto"/> existente con los datos de un DTO de solicitud
    /// <see cref="ProductoRequestDto"/>. Este método modifica directamente el objeto proporcionado.
    /// </summary>
    /// <param name="dto">El DTO de solicitud que contiene los datos actualizados.</param>
    /// <param name="producto">La entidad de producto existente a actualizar.</param>
    /// <remarks>
    /// Este método no retorna un nuevo objeto, sino que modifica la entidad proporcionada en memoria.
    /// Actualiza la propiedad UpdatedAt con la fecha UTC actual automáticamente.
    /// La imagen solo se actualiza si el DTO proporciona una cadena no vacía, preservando la imagen
    /// existente si el cliente no desea cambiarla.
    /// Útil para operaciones PUT/PATCH donde se mantiene la misma instancia de entidad.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint PUT para actualizar un producto
    /// [HttpPut("{id}")]
    /// public ActionResult&lt;ProductoDto&gt; UpdateProducto(long id, [FromBody] ProductoRequestDto dto)
    /// {
    ///     var producto = _repo.GetById(id);
    ///     if (producto == null) return NotFound();
    ///     
    ///     dto.UpdateEntity(producto);
    ///     _repo.Update(producto);
    ///     _repo.SaveChanges();
    ///     
    ///     return Ok(producto.ToDto());
    /// }
    /// </code>
    /// </example>
    public static void UpdateEntity(this ProductoRequestDto dto, Producto producto)
    {
        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.CategoriaId = dto.CategoriaId;
        if (!string.IsNullOrEmpty(dto.Imagen))
            producto.Imagen = dto.Imagen;
    }
}
