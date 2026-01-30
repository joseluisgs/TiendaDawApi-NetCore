using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaApi.Api.Models;

/// <summary>
/// Representa un producto en el catálogo de la tienda.
/// Persistido en PostgreSQL mediante Entity Framework Core.
/// </summary>
[Table("productos")]
public class Producto
{
    /// <summary>Clave primaria autoincremental.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>Nombre del producto (Máximo 100 caracteres).</summary>
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción comercial del producto.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Precio unitario de venta.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Precio { get; set; }

    /// <summary>Unidades disponibles en almacén.</summary>
    public int Stock { get; set; }

    /// <summary>Ruta del archivo de imagen o enlace externo.</summary>
    public string? Imagen { get; set; }

    /// <summary>Indica si el producto ha sido eliminado lógicamente.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>Relación con la categoría del producto.</summary>
    [ForeignKey("Categoria")]
    public long CategoriaId { get; set; }
    
    /// <summary>Navegación a la entidad Categoría.</summary>
    public virtual Categoria Categoria { get; set; } = default!;

    /// <summary>Versión de fila para control de concurrencia optimista.</summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    /// <summary>Fecha de alta automática.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de última actualización automática.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Determina si la imagen es un recurso local gestionado por el sistema.</summary>
    /// <returns>True si no es una URL externa.</returns>
    public bool IsLocalImage() => !string.IsNullOrEmpty(Imagen) && !Imagen.StartsWith("http");
}