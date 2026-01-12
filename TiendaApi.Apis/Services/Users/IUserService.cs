using CFE = CSharpFunctionalExtensions;
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
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    Task<CFE.Result<IEnumerable<UserDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// Returns: Result.Success(UserDto) | Result.Failure(NotFound)
    /// </summary>
    Task<CFE.Result<UserDto, DomainError>> FindByIdAsync(long id);

    /// <summary>
    /// Crea un nuevo usuario.
    /// Returns: Result.Success(UserDto) | Result.Failure(Validation/Conflict)
    /// </summary>
    Task<CFE.Result<UserDto, DomainError>> CreateAsync(RegisterDto dto);

    /// <summary>
    /// Actualiza un usuario existente.
    /// Returns: Result.Success(UserDto) | Result.Failure(NotFound/Validation/Conflict)
    /// </summary>
    Task<CFE.Result<UserDto, DomainError>> UpdateAsync(long id, UserUpdateDto dto);

    /// <summary>
    /// Elimina un usuario (soft delete).
    /// Returns: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    Task<CFE.UnitResult<DomainError>> DeleteAsync(long id);
}
