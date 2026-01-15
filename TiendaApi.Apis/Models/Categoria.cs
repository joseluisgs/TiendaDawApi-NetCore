namespace TiendaApi.Apis.Models;

using TiendaApi.Apis.Data;

/// <summary>
/// Entidad de dominio que representa una categoría de productos en el sistema.
/// 
/// <para>
/// Las categorías permiten organizar los productos en grupos lógicos para facilitar
/// la navegación y búsqueda en la tienda. Ejemplos comunes incluyen: "Electrónica",
/// "Ropa", "Libros", etc.
/// </para>
/// 
/// <para>
/// <b>Características principales:</b>
/// <list type="bullet">
///   <item><description>Identificador único auto-generado por la base de datos.</description></item>
///   <item><description>Nombre descriptivo (obligatorio, único por categoría activa).</description></item>
///   <item><description>Soporte para eliminación suave (soft-delete) sin perder datos históricos.</description></item>
///   <item><description>Relación uno-a-muchos con productos.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Patrón de datos:</b> Implementa <see cref="ITimestamped"/> para automáticamente
/// registrar las fechas de creación y actualización.
/// </para>
/// </summary>
/// <example>
/// Ejemplo de uso en código:
/// <code>
/// var categoria = new Categoria
/// {
///     Nombre = "Electrónica",
///     Productos = new List<Producto>()
/// };
/// </code>
/// </example>
public class Categoria : ITimestamped
{
    /// <summary>
    /// Identificador único de la categoría (clave primaria).
    /// 
    /// <para>
    /// Se genera automáticamente al guardar en la base de datos PostgreSQL
    /// mediante una secuencia (SERIAL o BIGSERIAL).
    /// </para>
    /// </summary>
    /// <remarks>
    /// Valores típicos: 1, 2, 3, ... (números positivos consecutivos)
    /// </remarks>
    public long Id { get; set; }

    /// <summary>
    /// Nombre descriptivo de la categoría.
    /// 
    /// <para>
    /// Este campo es obligatorio y debe ser único entre las categorías
    /// que no están eliminadas (IsDeleted = false).
    /// </para>
    /// <para>
    /// <b>Validaciones de negocio:</b>
    /// <list type="bullet">
    ///   <item><description>No puede estar vacío.</description></item>
    ///   <item><description>Longitud típica: 3-100 caracteres.</description></item>
    ///   <item><description>Debe ser único (case-insensitive en algunos casos).</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <example>
    /// Nombres válidos: "Electrónica", "ROPA", "Libros y Revistas"
    /// </example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la categoría ha sido eliminada (eliminación suave).
    /// 
    /// <para>
    /// En lugar de eliminar físicamente el registro de la base de datos,
    /// se marca este campo como true para mantener la integridad de datos
    /// y permitir auditoría histórica.
    /// </para>
    /// <para>
    /// Las consultas estándar filtran automáticamente por IsDeleted = false,
    /// por lo que las categorías eliminadas no aparecen en los listados.
    /// </para>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><term>false</term>: Categoría activa y visible.</item>
    ///   <item><term>true</term>: Categoría eliminada (soft-delete).</item>
    /// </list>
    /// </remarks>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Fecha y hora UTC de creación del registro.
    /// 
    /// <para>
    /// Se asigna automáticamente al crear la entidad mediante el interceptor
    /// <see cref="TimestampInterceptor"/> o directamente por la base de datos.
    /// </para>
    /// <remarks>
    /// Formato: DateTime en UTC (ej: 2024-01-15T10:30:00Z)
    /// </remarks>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora UTC de la última modificación.
    /// 
    /// <para>
    /// Se actualiza automáticamente cada vez que se modifica el registro.
    /// Si la entidad nunca ha sido modificada, coincide con CreatedAt.
    /// </para>
    /// <remarks>
    /// Importante para auditoría y para caches con invalidación basada en tiempo.
    /// </remarks>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Colección de productos asociados a esta categoría.
    /// 
    /// <para>
    /// Representa una relación uno-a-muchos: una categoría puede tener
    /// muchos productos, pero un producto pertenece a una sola categoría.
    /// </para>
    /// <remarks>
    /// La carga de esta propiedad depende de la configuración del DbContext
    /// (Eager Loading vs Lazy Loading).
    /// </remarks>
    public ICollection<Producto> Productos { get; set; } = [];
}
