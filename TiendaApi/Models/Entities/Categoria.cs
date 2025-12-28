namespace TiendaApi.Models.Entities;

/// <summary>
/// Entity representing a product category
/// Stored in PostgreSQL using Entity Framework Core
/// 
/// Java equivalent: @Entity from JPA
/// Spring Boot: Similar to Spring Data JPA entity
/// </summary>
public class Categoria
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property - similar to JPA @OneToMany
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
