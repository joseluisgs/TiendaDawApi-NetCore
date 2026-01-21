using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Usuarios;

/// <summary>
/// Implementación del repositorio de usuarios usando Entity Framework Core.
/// </summary>
public class UserRepository(
    TiendaDbContext context,
    ILogger<UserRepository> logger
) : IUserRepository
{

    /// <summary>
    /// Obtiene un usuario por su identificador.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>El usuario encontrado o null.</returns>
    public async Task<User?> FindByIdAsync(long id)
    {
        return await context.Users.FindAsync(id);
    }

    /// <summary>
    /// Obtiene un usuario por su nombre de usuario.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>El usuario encontrado o null.</returns>
    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// Obtiene un usuario por su correo electrónico.
    /// </summary>
    /// <param name="email">Correo electrónico.</param>
    /// <returns>El usuario encontrado o null.</returns>
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Obtiene todos los usuarios.
    /// </summary>
    /// <returns>Colección de todos los usuarios.</returns>
    public async Task<IEnumerable<User>> FindAllAsync()
    {
        return await context.Users.ToListAsync();
    }

    /// <summary>
    /// Obtiene usuarios paginados con filtros opcionales.
    /// </summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Tupla con los usuarios de la página y el total de registros.</returns>
    public async Task<(IEnumerable<User> Items, int TotalCount)> FindAllPagedAsync(UserFilterDto filter)
    {
        logger.LogDebug("Buscando usuarios paginados con filtros");

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

    /// <summary>
    /// Aplica ordenación a la consulta.
    /// </summary>
    private static IQueryable<User> ApplySorting(IQueryable<User> query, string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        Expression<Func<User, object>> keySelector = sortBy.ToLower() switch
        {
            "username" => u => u.Username,
            "email" => u => u.Email,
            "createdat" => u => u.CreatedAt,
            "updatedat" => u => u.UpdatedAt,
            _ => u => u.Id
        };

        return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    /// <summary>
    /// Guarda un nuevo usuario.
    /// </summary>
    /// <param name="user">Usuario a guardar.</param>
    /// <returns>El usuario guardado con fechas de creación y modificación.</returns>
    public async Task<User> SaveAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuario creado con id: {Id}", user.Id);

        return user;
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    /// <param name="user">Usuario con datos actualizados.</param>
    /// <returns>El usuario actualizado.</returns>
    public async Task<User> UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuario actualizado con id: {Id}", user.Id);

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
        if (user is not null)
        {
            user.IsDeleted = true;
            await context.SaveChangesAsync();

            logger.LogInformation("Usuario eliminado suavemente con id: {Id}", id);
        }
    }

    /// <summary>
    /// Obtiene todos los usuarios activos (no eliminados).
    /// </summary>
    /// <returns>Usuarios activos ordenados por Email.</returns>
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        logger.LogDebug("Obteniendo usuarios activos");

        return await context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }
}
