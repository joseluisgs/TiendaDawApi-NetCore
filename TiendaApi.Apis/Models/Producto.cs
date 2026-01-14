using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Models;

using TiendaApi.Apis.Data;

/// <summary>
/// Entidad de producto en la base de datos.
/// </summary>
public class Producto : ITimestamped
{
    /// <summary>
    /// URL de imagen por defecto para productos sin imagen.
    /// </summary>
    public const string IMAGE_DEFAULT = "https://via.placeholder.com/150";

    /// <summary>
    /// Prefijo de ruta para imágenes locales almacenadas.
    /// </summary>
    public const string IMAGE_LOCAL_PREFIX = "/storage/images/productos/";

    /// <summary>
    /// Identificador único del producto.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del producto.
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Precio del producto.
    /// </summary>
    public decimal Precio { get; set; }

    /// <summary>
    /// Cantidad en stock del producto.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// URL de la imagen del producto.
    /// Puede ser:
    /// - URL externa (http://, https://)
    /// - Ruta local (/storage/images/productos/filename.jpg)
    /// - IMAGE_DEFAULT si no tiene imagen
    /// </summary>
    public string? Imagen { get; set; }

    /// <summary>
    /// Indica si el producto está eliminado.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Fecha de creación del producto.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización del producto.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Versión para control de concurrencia optimista.
    /// Se actualiza automáticamente en cada modificación.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    /// <summary>
    /// Identificador de la categoría asociada al producto.
    /// </summary>
    public long CategoriaId { get; set; }

    /// <summary>
    /// Categoría asociada al producto.
    /// </summary>
    public Categoria Categoria { get; set; } = null!;

    /// <summary>
    /// Determina si la imagen del producto es local (almacenada en nuestro servidor).
    /// </summary>
    /// <returns>True si la imagen es local, false si es URL externa o por defecto.</returns>
    public bool IsLocalImage()
    {
        if (string.IsNullOrEmpty(Imagen))
            return false;

        // Es local si empieza con nuestro prefijo
        return Imagen.StartsWith(IMAGE_LOCAL_PREFIX, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determina si el producto tiene imagen por defecto.
    /// </summary>
    /// <returns>True si la imagen es la imagen por defecto.</returns>
    public bool HasDefaultImage()
    {
        return string.IsNullOrEmpty(Imagen) || Imagen == IMAGE_DEFAULT;
    }

    /// <summary>
    /// Obtiene la URL completa de la imagen para mostrar.
    /// Si es local, prepend /storage; si es externa, la retorna tal cual.
    /// </summary>
    /// <returns>URL completa de la imagen.</returns>
    public string GetImagenUrl()
    {
        if (string.IsNullOrEmpty(Imagen))
            return IMAGE_DEFAULT;

        // Si es URL externa (http/https), retornarla directamente
        if (Imagen.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            Imagen.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Imagen;

        // Si ya tiene el prefijo /storage, retornarla con /storage
        if (Imagen.StartsWith("/storage", StringComparison.OrdinalIgnoreCase))
            return Imagen;

        // Si es ruta local (/images/...), prepend /storage
        if (Imagen.StartsWith("/"))
            return $"/storage{Imagen}";

        // Si es solo el nombre del ficheo, prepend prefijo local
        return $"{IMAGE_LOCAL_PREFIX}{Imagen}";
    }
}
