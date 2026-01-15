using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Clase estática que proporciona métodos de extensión para el mapeo entre entidades de dominio y DTOs (Data Transfer Objects)
/// del modelo de Categoría.
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
/// <para><b>Ejemplo de uso general:</b></para>
/// <code>
/// // Convertir entidad a DTO para respuesta API
/// var categoriaDto = categoria.ToDto();
/// 
/// // Convertir lista de entidades a lista de DTOs
/// var categoriasDto = categorias.ToDtoList();
/// 
/// // Crear entidad desde DTO de solicitud
/// var categoria = dto.ToEntity();
/// 
/// // Actualizar entidad existente desde DTO
/// dto.UpdateEntity(categoria);
/// </code>
/// </summary>
public static class CategoriaMapper
{
    /// <summary>
    /// Convierte una entidad de dominio <see cref="Categoria"/> a un DTO de respuesta <see cref="CategoriaDto"/>
    /// para ser retornado en las respuestas de la API.
    /// </summary>
    /// <param name="categoria">La entidad de categoría a convertir.</param>
    /// <returns>Un nuevo objeto <see cref="CategoriaDto"/> con los datos de la categoría.</returns>
    /// <remarks>
    /// Este método es idempotente: no modifica el objeto original.
    /// Se utiliza principalmente en los endpoints GET para transformar entidades del dominio
    /// en objetos serializables para el cliente.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint de API
    /// [HttpGet("{id}")]
    /// public ActionResult&lt;CategoriaDto&gt; GetCategoria(long id)
    /// {
    ///     var categoria = _repo.GetById(id);
    ///     if (categoria == null) return NotFound();
    ///     return Ok(categoria.ToDto());
    /// }
    /// </code>
    /// </example>
    public static CategoriaDto ToDto(this Categoria categoria)
    {
        return new CategoriaDto(
            categoria.Id,
            categoria.Nombre,
            categoria.CreatedAt,
            categoria.UpdatedAt
        );
    }

    /// <summary>
    /// Convierte una colección de entidades de dominio <see cref="Categoria"/> a una colección de DTOs
    /// <see cref="CategoriaDto"/> para ser retornados en las respuestas de la API.
    /// </summary>
    /// <param name="categorias">La colección de entidades de categoría a convertir.</param>
    /// <returns>Una colección enumerable de objetos <see cref="CategoriaDto"/>.</returns>
    /// <remarks>
    /// Utiliza LINQ Select internamente para transformar cada elemento.
    /// Devuelve un IEnumerable&lt;CategoriaDto&gt; que se evalúa de forma diferida (lazy evaluation).
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint de API para listar todas las categorías
    /// [HttpGet]
    /// public ActionResult&lt;IEnumerable&lt;CategoriaDto&gt;&gt; GetCategorias()
    /// {
    ///     var categorias = _repo.GetAll();
    ///     return Ok(categorias.ToDtoList());
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<CategoriaDto> ToDtoList(this IEnumerable<Categoria> categorias)
    {
        return categorias.Select(c => c.ToDto());
    }

    /// <summary>
    /// Convierte un DTO de solicitud <see cref="CategoriaRequestDto"/> a una entidad de dominio <see cref="Categoria"/>
    /// para ser persistida en la base de datos.
    /// </summary>
    /// <param name="dto">El DTO de solicitud que contiene los datos proporcionados por el cliente.</param>
    /// <returns>Una nueva entidad <see cref="Categoria"/> con los datos del DTO.</returns>
    /// <remarks>
    /// Inicializa automáticamente las propiedades de auditoría CreatedAt y UpdatedAt con la fecha UTC actual.
    /// El ID se genera por la base de datos (autoincremento).
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint POST para crear una categoría
    /// [HttpPost]
    /// public ActionResult&lt;CategoriaDto&gt; CreateCategoria([FromBody] CategoriaRequestDto dto)
    /// {
    ///     var categoria = dto.ToEntity();
    ///     _repo.Add(categoria);
    ///     _repo.SaveChanges();
    ///     return CreatedAtAction(nameof(GetCategoria), new { id = categoria.Id }, categoria.ToDto());
    /// }
    /// </code>
    /// </example>
    public static Categoria ToEntity(this CategoriaRequestDto dto)
    {
        return new Categoria
        {
            Nombre = dto.Nombre,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Actualiza una entidad de dominio <see cref="Categoria"/> existente con los datos de un DTO de solicitud
    /// <see cref="CategoriaRequestDto"/>. Este método modifica directamente el objeto proporcionado.
    /// </summary>
    /// <param name="dto">El DTO de solicitud que contiene los datos actualizados.</param>
    /// <param name="categoria">La entidad de categoría existente a actualizar.</param>
    /// <remarks>
    /// Este método no retorna un nuevo objeto, sino que modifica la entidad proporcionada en memoria.
    /// Actualiza la propiedad UpdatedAt con la fecha UTC actual automáticamente.
    /// Útil para operaciones PUT/PATCH donde se mantiene la misma instancia de entidad.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint PUT para actualizar una categoría
    /// [HttpPut("{id}")]
    /// public ActionResult&lt;CategoriaDto&gt; UpdateCategoria(long id, [FromBody] CategoriaRequestDto dto)
    /// {
    ///     var categoria = _repo.GetById(id);
    ///     if (categoria == null) return NotFound();
    ///     
    ///     dto.UpdateEntity(categoria);
    ///     _repo.Update(categoria);
    ///     _repo.SaveChanges();
    ///     
    ///     return Ok(categoria.ToDto());
    /// }
    /// </code>
    /// </example>
    public static void UpdateEntity(this CategoriaRequestDto dto, Categoria categoria)
    {
        categoria.Nombre = dto.Nombre;
    }
}
