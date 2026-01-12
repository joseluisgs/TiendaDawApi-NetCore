namespace TiendaApi.Apis.Models;

/// <summary>
/// Entidad de categoría en la base de datos.
/// </summary>
public class Categoria
{
    /// <summary>
    /// Identificador único de la categoría.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Nombre de la categoría.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>
    /// Indica si la categoría está eliminada.
    /// </summary>
    public bool IsDeleted { get; set; }
    /// <summary>
    /// Fecha de creación de la categoría.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Fecha de última actualización de la categoría.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Colección de productos asociados a esta categoría.
    /// </summary>
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
