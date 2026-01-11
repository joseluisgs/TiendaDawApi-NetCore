using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models;

namespace TiendaApi.Repositories.Usuarios;

/// <summary>
/// Implementación del repositorio de usuarios usando Entity Framework Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly TiendaDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(TiendaDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un usuario por su identificador.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>El usuario encontrado o null.</returns>
    public async Task<User?> FindByIdAsync(long id)
    {
        return await _context.Users.FindAsync(id);
    }

    /// <summary>
    /// Obtiene un usuario por su nombre de usuario.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>El usuario encontrado o null.</returns>
    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// Obtiene un usuario por su correo electrónico.
    /// </summary>
    /// <param name="email">Correo electrónico.</param>
    /// <returns>El usuario encontrado o null.</returns>
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Obtiene todos los usuarios.
    /// </summary>
    /// <returns>Colección de todos los usuarios.</returns>
    public async Task<IEnumerable<User>> FindAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    /// <summary>
    /// Guarda un nuevo usuario.
    /// </summary>
    /// <param name="user">Usuario a guardar.</param>
    /// <returns>El usuario guardado con fechas de creación y modificación.</returns>
    public async Task<User> SaveAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Usuario creado con id: {Id}", user.Id);
        
        return user;
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    /// <param name="user">Usuario con datos actualizados.</param>
    /// <returns>El usuario actualizado.</returns>
    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Usuario actualizado con id: {Id}", user.Id);
        
        return user;
    }

    /// <summary>
    /// Elimina un usuario por su identificador (eliminación suave).
    /// </summary>
    /// <param name="id">Identificador del usuario a eliminar.</param>
    /// <returns>Tarea asíncrona.</returns>
    public async Task DeleteAsync(long id)
    {
        var user = await FindByIdAsync(id);
        if (user != null)
        {
            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Usuario eliminado suavemente con id: {Id}", id);
        }
    }
}
