using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Models;

using TiendaApi.Apis.Data;

/// <summary>
/// Entidad de dominio que representa un producto en el catálogo de la tienda.
/// 
/// <para>
/// Un producto es el elemento central del sistema de comercio electrónico.
/// Contiene toda la información necesaria para、展示, venta y gestión del inventario.
/// </para>
/// 
/// <para>
/// <b>Características principales:</b>
/// <list type="bullet">
///   <item><description>Identificador único auto-generado.</description></item>
///   <item><description>Información de inventario (stock) para control de ventas.</description></item>
///   <item><description>Gestión flexible de imágenes (locales o externas).</description></item>
///   <item><description>Relación con categoría para organización.</description></item>
///   <item><description>Control de concurrencia optimista mediante RowVersion.</description></item>
/// </list>
/// </para>
/// </summary>
public class Producto : ITimestamped
{
    /// <summary>
    /// URL de imagen por defecto para productos sin imagen personalizada.
    /// 
    /// <para>
    /// Se usa cuando el campo Imagen es nulo o vacío, proporcionando
    /// una imagen de marcador de posición visualmente coherente.
    /// </para>
    /// </summary>
    public const string IMAGE_DEFAULT = "https://via.placeholder.com/150";

    /// <summary>
    /// Prefijo de ruta para imágenes locales almacenadas en el servidor.
    /// 
    /// <para>
    /// Las imágenes cargadas por usuarios se almacenan en la carpeta
    /// /storage/images/productos/ y se referencian con este prefijo.
    /// </para>
    /// </summary>
    public const string IMAGE_LOCAL_PREFIX = "/storage/images/productos/";

    /// <summary>
    /// Identificador único del producto (clave primaria).
    /// 
    /// <para>
    /// Se genera automáticamente al guardar en la base de datos PostgreSQL.
    /// Es el identificador usado en URLs y referencias externas.
    /// </para>
    /// <remarks>
    /// Valor ejemplo: 1, 2, 3, ... (números positivos)
    /// </remarks>
    public long Id { get; set; }

    /// <summary>
    /// Nombre del producto.
    /// 
    /// <para>
    /// Campo obligatorio que identifica el producto de forma legible.
    /// Se usa en búsquedas, listados y detalles del producto.
    /// </para>
    /// <remarks>
    /// Longitud típica: 3-200 caracteres
    /// </remarks>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada del producto.
    /// 
    /// <para>
    /// Proporciona información adicional sobre características,
    /// especificaciones, materiales, dimensiones, etc.
    /// </para>
    /// <remarks>
    /// Puede estar vacío si el nombre es suficientemente descriptivo.
    /// </remarks>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Precio unitario del producto en la moneda configurada (EUR).
    /// 
    /// <para>
    /// Puede incluir decimales para precios con céntimos.
    /// Se valida que sea mayor o igual a 0.
    /// </para>
    /// <remarks>
    /// Formato: decimal con hasta 2 decimales (ej: 19.99)
    /// </remarks>
    public decimal Precio { get; set; }

    /// <summary>
    /// Cantidad disponible en inventario.
    /// 
    /// <para>
    /// Representa el stock físico disponible para venta.
    /// Se decrementa automáticamente al crear pedidos.
    /// </para>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><term>0</term>: Sin stock (no se puede comprar).</item>
    ///   <item><term>&gt; 0</term>: Stock disponible.</item>
    ///   <item><term>Negativo</term>: Permite backorders (pre-pedidos).</item>
    /// </list>
    /// </remarks>
    public int Stock { get; set; }

    /// <summary>
    /// URL o ruta de la imagen del producto.
    /// 
    /// <para>
    /// Puede ser de tres tipos:
    /// <list type="bullet">
    ///   <item><description>URL externa (http://, https://): Imágenes de CDNs o servicios.</description></item>
    ///   <item><description>Ruta local (/storage/images/...): Imágenes cargadas por usuarios.</description></item>
    ///   <item><description>Nulo o IMAGE_DEFAULT: Sin imagen personalizada.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <example>
    /// Valores válidos:
    /// "https://cdn.example.com/producto.jpg"
    /// "/storage/images/productos/123456.jpg"
    /// null (usa IMAGE_DEFAULT)
    /// </example>
    public string? Imagen { get; set; }

    /// <summary>
    /// Indica si el producto ha sido eliminado (soft-delete).
    /// 
    /// <para>
    /// Los productos eliminados no aparecen en búsquedas ni listados,
    /// pero los datos históricos se mantienen para pedidos existentes.
    /// </para>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Fecha y hora UTC de creación del registro.
    /// 
    /// <para>
    /// Se asigna automáticamente al crear el producto.
    /// Se usa para ordenación y auditoría.
    /// </para>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora UTC de la última modificación.
    /// 
    /// <para>
    /// Se actualiza cada vez que se modifica el producto.
    /// Si nunca se modificó, coincide con CreatedAt.
    /// </para>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Token de versión para control de concurrencia optimista.
    /// 
    /// <para>
    /// Entity Framework actualiza este campo automáticamente cada vez que
    /// se modifica el registro. Si dos usuarios modifican simultáneamente,
    /// el segundo recibe un error de concurrencia.
    /// </para>
    /// <remarks>
    /// Implementado como columna TIMESTAMP en PostgreSQL (rowversion).
    /// </remarks>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    /// <summary>
    /// Identificador de la categoría a la que pertenece el producto.
    /// 
    /// <para>
    /// Clave foránea que establece la relación con Categoría.
    /// Si es 0, el producto no tiene categoría asignada.
    /// </para>
    public long CategoriaId { get; set; }

    /// <summary>
    /// Categoría asociada al producto (carga lazy o eager).
    /// 
    /// <para>
    /// Relación muchos-a-uno: muchos productos pueden pertenecer
    /// a una misma categoría.
    /// </para>
    public Categoria Categoria { get; set; } = null!;

    /// <summary>
    /// Determina si la imagen del producto es local (almacenada en el servidor).
    /// 
    /// <para>
    /// Las imágenes locales requieren manejo especial para servir
    /// archivos estáticos y limpieza al eliminar el producto.
    /// </para>
    /// <returns>
    /// <see langword="true"/> si la imagen es local (comienza con IMAGE_LOCAL_PREFIX),
    /// <see langword="false"/> si es URL externa o no tiene imagen.
    /// </returns>
    /// <example>
    /// Uso típico:
    /// <code>
    /// if (producto.IsLocalImage())
    ///     await storageService.DeleteFileAsync(producto.Imagen);
    /// </code>
    /// </example>
    public bool IsLocalImage()
    {
        if (string.IsNullOrEmpty(Imagen))
            return false;

        return Imagen.StartsWith(IMAGE_LOCAL_PREFIX, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determina si el producto usa la imagen por defecto.
    /// 
    /// <para>
    /// Útil para mostrar un badge "Sin imagen" en la interfaz
    /// o para incentivar la carga de imágenes.
    /// </para>
    /// <returns>
    /// <see langword="true"/> si Imagen es null, vacío o igual a IMAGE_DEFAULT.
    /// </returns>
    public bool HasDefaultImage()
    {
        return string.IsNullOrEmpty(Imagen) || Imagen == IMAGE_DEFAULT;
    }

    /// <summary>
    /// Obtiene la URL completa de la imagen lista para mostrar en navegador.
    /// 
    /// <para>
    /// Normaliza diferentes formatos de entrada:
    /// <list type="number">
    ///   <item><description>URLs externas (http/https): retornadas sin modificación.</description></item>
    ///   <item><description>Rutas con /storage: retornadas con prefijo.</description></item>
    ///   <item><description>Rutas relativas (/images/...): prepend /storage.</description></item>
    ///   <item><description>Nombres de archivo: prepend IMAGE_LOCAL_PREFIX.</description></item>
    ///   <item><description>Sin imagen: retorna IMAGE_DEFAULT.</description></item>
    /// </list>
    /// </para>
    /// <returns>URL absoluta o relativa lista para usar en etiquetas &lt;img src="..."&gt;.</returns>
    /// <example>
    /// Uso en Razor/HTML:
    /// <code>
    /// &lt;img src="@producto.GetImagenUrl()" alt="@producto.Nombre" /&gt;
    /// </code>
    /// </example>
    public string GetImagenUrl()
    {
        if (string.IsNullOrEmpty(Imagen))
            return IMAGE_DEFAULT;

        if (Imagen.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            Imagen.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Imagen;

        if (Imagen.StartsWith("/storage", StringComparison.OrdinalIgnoreCase))
            return Imagen;

        if (Imagen.StartsWith("/"))
            return $"/storage{Imagen}";

        return $"{IMAGE_LOCAL_PREFIX}{Imagen}";
    }
}
