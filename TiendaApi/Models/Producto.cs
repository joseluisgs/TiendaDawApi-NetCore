namespace TiendaApi.Models;

/// <summary>
/// Entidad de producto en la base de datos.
/// </summary>
public class Producto
{
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
    /// </summary>
    public string? Imagen { get; set; }
    /// <summary>
    /// Indica si el producto está eliminado.
    /// </summary>
    public bool IsDeleted { get; set; }
    /// <summary>
    /// Fecha de creación del producto.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Fecha de última actualización del producto.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador de la categoría asociada al producto.
    /// </summary>
    public long CategoriaId { get; set; }
    /// <summary>
    /// Categoría asociada al producto.
    /// </summary>
    public Categoria Categoria { get; set; } = null!;
}
