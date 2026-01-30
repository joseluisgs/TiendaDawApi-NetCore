using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Api.Models;

/// <summary>
/// Agrupación lógica de productos.
/// </summary>
[Table("categorias")]
public class Categoria
{
    /// <summary>Clave primaria.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>Nombre de la categoría (Único).</summary>
    [Required]
    [MaxLength(50)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Indica si la categoría ha sido eliminada.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>Fecha de creación.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de actualización.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Colección de productos que pertenecen a esta categoría.</summary>
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}