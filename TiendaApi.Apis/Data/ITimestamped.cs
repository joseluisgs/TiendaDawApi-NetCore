namespace TiendaApi.Apis.Data;

/// <summary>
/// Interfaz de auditoría para entidades que requieren tracking de tiempo.
/// 
/// <para>
/// Define un contrato para entidades que necesitan mantener fechas de
/// creación y modificación automáticas. Es parte del patrón de auditoría
/// del sistema que garantiza trazabilidad completa de los cambios.
/// </para>
/// 
/// <para>
/// <b>Propósito:</b> Estandarizar cómo las entidades registran su ciclo de vida,
/// facilitando:
/// <list type="bullet">
///   <item><description>Auditoría de cambios en datos sensibles.</description></item>
/// <item><description>Debugging y resolución de problemas.</description></item>
/// <item><description>Análisis de uso y patrones de acceso.</description></item>
/// <item><description>Invalidación de caches basada en tiempo.</description></item>
/// <item><description>Cumplimiento con normativas de protección de datos.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Implementación:</b> EF Core asigna automáticamente estos campos:
/// <list type="bullet">
///   <item><description>CreatedAt: Se asigna al INSERT, nunca cambia después.</description></item>
///   <item><description>UpdatedAt: Se asigna/actualiza en INSERT y UPDATE.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Patrón de implementación:</b> Se utiliza un interceptor (TimestampInterceptor)
/// que automáticamente inyecta los valores antes de persistir en la base de datos.
/// Esto garantiza consistencia incluso en operaciones directas al DbContext.
/// </para>
/// 
/// <para>
/// <b>Entidades que implementan esta interfaz:</b>
/// <list type="bullet">
///   <item><description><see cref="Models.Categoria"/>: Categorías de productos.</description></item>
///   <item><description><see cref="Models.Producto"/>: Productos del catálogo.</description></item>
///   <item><description><see cref="Models.User"/>: Usuarios del sistema.</description></item>
///   <item><description><see cref="Models.Pedido"/>: Pedidos de clientes.</description></item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// Verificar si una entidad ha sido modificada:
///
/// <code>
/// var entidad = await dbContext.Productos.FindAsync(id);
/// bool modificado = entidad.CreatedAt != entidad.UpdatedAt;
/// </code>
///
/// Ordenar por fecha de creación:
///
/// <code>
/// var recientes = await dbContext.Productos
///     .Where(p => !p.IsDeleted)
///     .OrderByDescending(p => p.CreatedAt)
///     .Take(10)
///     .ToListAsync();
/// </code>
/// </example>
public interface ITimestamped
{
    /// <summary>
    /// Fecha y hora UTC de creación del registro.
    /// 
    /// <para>
    /// Se asigna automáticamente cuando se crea el registro por primera vez.
    /// Este valor nunca cambia durante toda la vida del registro.
    /// </para>
    /// 
    /// <para>
    /// <b>Uso común:</b>
    /// <list type="bullet">
    ///   <item><description>Ordenación cronológica de registros.</description></item>
    ///   <item><description>Cálculo de antigüedad de datos.</description></item>
    ///   <item><description>Auditoría y compliance.</description></item>
    ///   <item><description>Debugging de problemas de sincronización.</description></item>
    /// </list>
    /// </para>
    /// 
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>Tipo: <see cref="System.DateTime"/> (UTC).</description></item>
    ///   <item><description>Persistente: Sí (columna TIMESTAMP o equivalente).</description></item>
    ///   <item><description>Editable: No (se ignora en Updates).</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Valor ejemplo: 2024-01-15T10:30:00Z
    /// </example>
    DateTime CreatedAt { get; init; }

    /// <summary>
    /// Fecha y hora UTC de la última modificación del registro.
    /// 
    /// <para>
    /// Se actualiza automáticamente cada vez que el registro se modifica.
    /// Si el registro nunca ha sido modificado, coincide con CreatedAt.
    /// </para>
    /// 
    /// <para>
    /// <b>Uso común:</b>
    /// <list type="bullet">
    ///   <item><description>Invalidación de caches (ETag, Last-Modified).</description></item>
    ///   <item><description>Detección de cambios concurrentes.</description></item>
    ///   <item><description>Análisis de actividad reciente.</description></item>
    ///   <item><description>Resolución de conflictos en sincronización.</description></item>
    /// </list>
    /// </para>
    /// 
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>Tipo: <see cref="System.DateTime"/> (UTC).</description></item>
    ///   <item><description>Persistente: Sí (columna TIMESTAMP o equivalente).</description></item>
    ///   <item><description>Editable: No (auto-actualizado por el interceptor).</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Valor ejemplo: 2024-01-15T14:45:00Z (diferente de CreatedAt si se modificó)
    /// </example>
    DateTime UpdatedAt { get; init; }
}
