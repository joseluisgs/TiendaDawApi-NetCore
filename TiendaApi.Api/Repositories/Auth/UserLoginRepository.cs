using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Auth;

/// <summary>
/// Implementación del repositorio de vinculaciones OAuth.
/// </summary>
public class UserLoginRepository(
    TiendaDbContext context,
    ILogger<UserLoginRepository> logger
) : IUserLoginRepository
{
    /// <inheritdoc/>
    public async Task<UserLogin?> FindByProviderAsync(string loginProvider, string providerKey)
    {
        return await context.UserLogins
            .FirstOrDefaultAsync(ul =>
                ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserLogin>> FindByUserIdAsync(long userId)
    {
        return await context.UserLogins
            .Where(ul => ul.UserId == userId)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<UserLogin> SaveAsync(UserLogin userLogin)
    {
        context.UserLogins.Add(userLogin);
        await context.SaveChangesAsync();
        logger.LogInformation("Vinculación OAuth creada: {Provider}/{Key} → UserId {UserId}",
            userLogin.LoginProvider, userLogin.ProviderKey, userLogin.UserId);
        return userLogin;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string loginProvider, string providerKey)
    {
        return await context.UserLogins
            .AnyAsync(ul =>
                ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey);
    }
}
