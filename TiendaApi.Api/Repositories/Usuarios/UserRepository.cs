using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Usuarios;

/// <summary>
/// Implementación del repositorio de usuarios.
/// </summary>
public class UserRepository(
    TiendaDbContext context,
    ILogger<UserRepository> logger
) : IUserRepository
{
    /// <inheritdoc/>
    public async Task<User?> FindByIdAsync(long id)
    {
        return await context.Users.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> FindAllAsync()
    {
        return await context.Users.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<User> Items, int TotalCount)> FindAllPagedAsync(UserFilterDto filter)
    {
        logger.LogDebug("Buscando usuarios paginados");

        var query = filter.IsDeleted.HasValue
            ? context.Users.IgnoreQueryFilters().AsQueryable()
            : context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Username))
            query = query.Where(u => EF.Functions.Like(u.Username, $"%{filter.Username}%"));

        if (!string.IsNullOrWhiteSpace(filter.Email))
            query = query.Where(u => EF.Functions.Like(u.Email, $"%{filter.Email}%"));

        if (filter.IsDeleted.HasValue)
            query = query.Where(u => u.IsDeleted == filter.IsDeleted.Value);

        var totalCount = await query.CountAsync();
        query = ApplySorting(query, filter.SortBy, filter.Direction);

        var items = await query
            .Skip(filter.Page * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<User> SaveAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        logger.LogInformation("Usuario creado con ID: {Id}", user.Id);
        return user;
    }

    /// <inheritdoc/>
    public async Task<User> UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
        logger.LogInformation("Usuario actualizado con ID: {Id}", user.Id);
        return user;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(long id)
    {
        var user = await FindByIdAsync(id);
        if (user is not null)
        {
            user.IsDeleted = true;
            await context.SaveChangesAsync();
            logger.LogInformation("Usuario eliminado con ID: {Id}", id);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        logger.LogDebug("Obteniendo usuarios activos");
        return await context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        Expression<Func<User, object>> keySelector = sortBy.ToLower() switch
        {
            "username" => u => u.Username,
            "email" => u => u.Email,
            "createdat" => u => u.CreatedAt,
            _ => u => u.Id
        };
        return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
