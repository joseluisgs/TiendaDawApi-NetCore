using TiendaApi.Models;

namespace TiendaApi.Repositories.Usuarios;

/// <summary>
/// Repository interface for User entity
/// Java Spring equivalent: extends JpaRepository<User, Long>
/// </summary>
public interface IUserRepository
{
    Task<User?> FindByIdAsync(long id);
    Task<User?> FindByUsernameAsync(string username);
    Task<User?> FindByEmailAsync(string email);
    Task<IEnumerable<User>> FindAllAsync();
    Task<User> SaveAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(long id);
}
