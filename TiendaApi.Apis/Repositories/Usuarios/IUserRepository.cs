using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Usuarios;

/// <summary>
/// Interfaz del repositorio de usuarios.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Obtiene un usuario por su identificador.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> FindByIdAsync(long id);

    /// <summary>
    /// Obtiene un usuario por su nombre de usuario.
    /// </summary>
    /// <param name="username">Nombre de usuario.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> FindByUsernameAsync(string username);

    /// <summary>
    /// Obtiene un usuario por su correo electrónico.
    /// </summary>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> FindByEmailAsync(string email);

    /// <summary>
    /// Obtiene todos los usuarios.
    /// </summary>
    /// <returns>Colección de todos los usuarios.</returns>
    Task<IEnumerable<User>> FindAllAsync();

    /// <summary>
    /// Obtiene usuarios paginados con filtros opcionales.
    /// </summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Tupla con los usuarios de la página y el total de registros.</returns>
    Task<(IEnumerable<User> Items, int TotalCount)> FindAllPagedAsync(UserFilterDto filter);

    /// <summary>
    /// Guarda un nuevo usuario.
    /// </summary>
    /// <param name="user">Usuario a guardar.</param>
    /// <returns>El usuario guardado con los datos actualizados.</returns>
    Task<User> SaveAsync(User user);

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    /// <param name="user">Usuario con los datos actualizados.</param>
    /// <returns>El usuario actualizado.</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Elimina un usuario por su identificador (eliminación suave).
    /// </summary>
    /// <param name="id">Identificador del usuario a eliminar.</param>
    /// <returns>Tarea asíncrona.</returns>
    Task DeleteAsync(long id);
}
