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
        // AsNoTracking: la vinculacion hallada solo se lee (UserId); si no
        // existe, la alta es un Add de una entidad NUEVA en SaveAsync.
        return await context.ExternalLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(ul =>
                ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserLogin>> FindByUserIdAsync(long userId)
    {
        return await context.ExternalLogins
            .AsNoTracking()
            .Where(ul => ul.UserId == userId)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<UserLogin> SaveAsync(UserLogin userLogin)
    {
        context.ExternalLogins.Add(userLogin);
        await context.SaveChangesAsync();
        logger.LogInformation("Vinculación OAuth creada: {Provider}/{Key} → UserId {UserId}",
            userLogin.LoginProvider, userLogin.ProviderKey, userLogin.UserId);
        return userLogin;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string loginProvider, string providerKey)
    {
        return await context.ExternalLogins
            .AnyAsync(ul =>
                ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey);
    }
}
