using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Models;

using TiendaApi.Api.Data.Abstractions;

/// <summary>
/// Entidad de dominio que representa un producto en el catálogo.
/// </summary>
public class Producto : ITimestamped
{
    /// <summary>URL de imagen por defecto cuando no hay imagen personalizada.</summary>
    public const string IMAGE_DEFAULT = "https://via.placeholder.com/150";

    /// <summary>Prefijo para imágenes locales (/storage/uploads/productos/).</summary>
    public const string IMAGE_LOCAL_PREFIX = "/storage/uploads/productos/";

    /// <summary>ID único del producto (PK en PostgreSQL).</summary>
    public long Id { get; set; }

    /// <summary>Nombre del producto (3-200 caracteres).</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción detallada del producto.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Precio unitario en EUR (decimal con hasta 2 decimales).</summary>
    public decimal Precio { get; set; }

    /// <summary>Stock disponible (0 = sin stock, >0 = disponible, <0 = backorder).</summary>
    public int Stock { get; set; }

    /// <summary>URL o ruta de la imagen del producto (null = usa IMAGE_DEFAULT).</summary>
    public string? Imagen { get; set; }

    /// <summary>Indica si el producto está eliminado (soft-delete).</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Token de control de concurrencia optimista (bytea en PostgreSQL).</summary>
    public byte[] RowVersion { get; set; } = new byte[8];

    /// <summary>ID de la categoría asociada (FK).</summary>
    public long CategoriaId { get; set; }

    /// <summary>Categoría asociada (relación muchos-a-uno).</summary>
    public Categoria Categoria { get; set; } = null!;

    /// <summary>
    /// Determina si la imagen es local (almacenada en el servidor).
    /// </summary>
    /// <returns>true si la imagen comienza con IMAGE_LOCAL_PREFIX.</returns>
    public bool IsLocalImage()
    {
        if (string.IsNullOrEmpty(Imagen))
            return false;

        return Imagen.StartsWith(IMAGE_LOCAL_PREFIX, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determina si el producto usa la imagen por defecto.
    /// </summary>
    /// <returns>true si Imagen es null, vacío o igual a IMAGE_DEFAULT.</returns>
    public bool HasDefaultImage()
    {
        return string.IsNullOrEmpty(Imagen) || Imagen == IMAGE_DEFAULT;
    }

    /// <summary>
    /// Obtiene la URL completa de la imagen normalizada para mostrar.
    /// </summary>
    /// <returns>URL absoluta o relativa lista para usar en &lt;img src&gt;.</returns>
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
