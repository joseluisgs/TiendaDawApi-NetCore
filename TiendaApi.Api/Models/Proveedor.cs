namespace TiendaApi.Api.Models;

using TiendaApi.Api.Data.Abstractions;

/// <summary>
/// Proveedor de productos. Entidad administrada exclusivamente por el rol ADMIN.
///
/// Su clave primaria es un <c>string</c> de 11 caracteres generado por EF Core con
/// <see cref="IdGenerators.YouTubeIdValueGenerator"/>: identificadores no correlativos,
/// distribuibles y legibles en URL. Un proveedor tiene muchos productos; cada producto
/// apunta a uno solo o a ninguno.
/// </summary>
public class Proveedor : ITimestamped {
    /// <summary>
    /// Clave primaria: identificador estilo YouTube (11 caracteres Base64url).
    /// Se declara sin inicializar a propósito: si llegara con un valor (incluso vacío),
    /// EF Core no llamaría al generador y guardaría ese valor tal cual.
    /// </summary>
    /// <example>XqZsoesa55w</example>
    public string Codigo { get; set; } = null!;

    /// <summary>Nombre comercial del proveedor. Debe ser único en el sistema.</summary>
    /// <example>Distribuciones DAW</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción opcional del proveedor.</summary>
    public string? Descripcion { get; set; }

    /// <summary>Dato de contacto: email, teléfono o persona de referencia.</summary>
    public string? Contacto { get; set; }

    /// <summary>Marca de borrado lógico: los filtros globales ocultan la fila sin borrarla.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha y hora de creación en UTC, gestionada por el interceptor de marcas de tiempo.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha y hora de última modificación en UTC, gestionada por el interceptor.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Productos asociados a este proveedor (relación 1 a N).</summary>
    public ICollection<Producto> Productos { get; set; } = [];
}
