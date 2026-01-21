using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Users;

/// <summary>
/// Contrato del servicio de usuarios.
/// </summary>
public interface IUserService
{
    /// <summary>Obtiene todos los usuarios activos.</summary>
    /// <returns>Resultado con colección de usuarios.</returns>
    Task<Result<IEnumerable<UserDto>, DomainError>> FindAllAsync();

    /// <summary>Obtiene usuarios paginados con filtros.</summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Resultado con usuarios paginados.</returns>
    Task<Result<PagedResult<UserDto>, DomainError>> FindAllPagedAsync(UserFilterDto filter);

    /// <summary>Busca un usuario por ID.</summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>Resultado con el usuario o error.</returns>
    Task<Result<UserDto, DomainError>> FindByIdAsync(long id);

    /// <summary>Registra un nuevo usuario.</summary>
    /// <param name="dto">Datos de registro.</param>
    /// <returns>Resultado con el usuario creado.</returns>
    Task<Result<UserDto, DomainError>> CreateAsync(RegisterDto dto);

    /// <summary>Actualiza un usuario existente.</summary>
    /// <param name="id">ID del usuario.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Resultado con el usuario actualizado.</returns>
    Task<Result<UserDto, DomainError>> UpdateAsync(long id, UserUpdateDto dto);

    /// <summary>Actualiza el avatar de un usuario.</summary>
    /// <param name="id">ID del usuario.</param>
    /// <param name="avatarUrl">URL del avatar.</param>
    /// <returns>Resultado con el usuario actualizado.</returns>
    Task<Result<UserDto, DomainError>> UpdateAvatarAsync(long id, string avatarUrl);

    /// <summary>Elimina un usuario (soft delete).</summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteAsync(long id);
}
