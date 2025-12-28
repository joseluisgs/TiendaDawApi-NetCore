using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models.Entities;

namespace TiendaApi.Repositories;

/// <summary>
/// Repository implementation for User entity using Entity Framework Core
/// Java Spring equivalent: JpaRepository<User, Long> implementation
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

    public async Task<User?> FindByIdAsync(long id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> FindAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> SaveAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User created with id: {Id}", user.Id);
        
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User updated with id: {Id}", user.Id);
        
        return user;
    }

    public async Task DeleteAsync(long id)
    {
        var user = await FindByIdAsync(id);
        if (user != null)
        {
            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User soft-deleted with id: {Id}", id);
        }
    }
}
