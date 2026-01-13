using CFE = CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Users;

/// <summary>
/// Interfaz del servicio de usuarios usando Patrón Result.
/// Maneja las operaciones CRUD de usuarios con Programación Orientada al Resultado.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Obtiene todos los usuarios (excluyendo eliminados).
    /// Devuelve: Result.Success(List) | Result.Failure nunca
    /// </summary>
    Task<CFE.Result<IEnumerable<UserDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene usuarios paginados (excluyendo eliminados).
    /// Devuelve: Result.Success(PagedResult) | Result.Failure nunca
    /// </summary>
    Task<CFE.Result<PagedResult<UserDto>, DomainError>> FindAllPagedAsync(int page, int pageSize);

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(NotFound)
    /// </summary>
    Task<CFE.Result<UserDto, DomainError>> FindByIdAsync(long id);

    /// <summary>
    /// Crea un nuevo usuario.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(Validation/Conflict)
    /// </summary>
    Task<CFE.Result<UserDto, DomainError>> CreateAsync(RegisterDto dto);

    /// <summary>
    /// Actualiza un usuario existente.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(NotFound/Validation/Conflict)
    /// </summary>
    Task<CFE.Result<UserDto, DomainError>> UpdateAsync(long id, UserUpdateDto dto);

    /// <summary>
    /// Actualiza el avatar de un usuario.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    Task<CFE.Result<UserDto, DomainError>> UpdateAvatarAsync(long id, string avatarUrl);

    /// <summary>
    /// Elimina un usuario (soft delete).
    /// Devuelve: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    Task<CFE.UnitResult<DomainError>> DeleteAsync(long id);
}
