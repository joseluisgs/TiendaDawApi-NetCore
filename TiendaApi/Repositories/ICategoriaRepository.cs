using TiendaApi.Models.Entities;

namespace TiendaApi.Repositories;

/// <summary>
/// Repository interface for Categoria
/// Generic CRUD operations
/// 
/// Java equivalent: extends JpaRepository<Categoria, Long>
/// Spring Boot: Interface that Spring Data implements automatically
/// 
/// In C#: We implement these manually or use generic repository pattern
/// </summary>
public interface ICategoriaRepository
{
    Task<IEnumerable<Categoria>> FindAllAsync();
    Task<Categoria?> FindByIdAsync(long id);
    Task<Categoria> SaveAsync(Categoria categoria);
    Task<Categoria> UpdateAsync(Categoria categoria);
    Task DeleteAsync(long id);
    Task<bool> ExistsByNombreAsync(string nombre, long? excludeId = null);
}
