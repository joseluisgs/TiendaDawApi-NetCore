using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Auth;

/// <summary>
/// Contrato del repositorio de vinculaciones OAuth (logins externos).
/// </summary>
public interface IUserLoginRepository
{
    /// <summary>Busca una vinculación por proveedor y provider key.</summary>
    Task<UserLogin?> FindByProviderAsync(string loginProvider, string providerKey);

    /// <summary>Guarda una nueva vinculación.</summary>
    Task<UserLogin> SaveAsync(UserLogin userLogin);

    /// <summary>Verifica si ya existe la vinculación.</summary>
    Task<bool> ExistsAsync(string loginProvider, string providerKey);
}
