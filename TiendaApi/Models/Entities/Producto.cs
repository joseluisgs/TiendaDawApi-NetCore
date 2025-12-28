namespace TiendaApi.Models.Entities;

/// <summary>
/// Entity representing a product
/// Stored in PostgreSQL using Entity Framework Core
/// 
/// Java equivalent: @Entity from JPA with @ManyToOne relationship
/// </summary>
public class Producto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? Imagen { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key to Categoria - similar to JPA @ManyToOne
    public long CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
}
