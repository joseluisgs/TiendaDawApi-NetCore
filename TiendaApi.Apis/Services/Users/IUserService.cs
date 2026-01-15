using CFE = CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Users;

/// <summary>
/// Interfaz del servicio de usuarios que implementa el patrón de arquitectura por capas (Service Layer).
/// Gestiona toda la lógica de negocio relacionada con usuarios: CRUD, gestión de avatares y operaciones de cuenta.
/// Este servicio sigue el patrón de Programación Orientada al Resultado (Result-Oriented Programming) para
/// un manejo explícito y tipado de operaciones exitosas y fallidas.
///
/// <para><b>Patrón Service Layer:</b></para>
/// <list type="bullet">
///   <item><description>Abstrae la lógica de negocio de los detalles de infraestructura</description></item>
///   <item><description>Coordina operaciones entre diferentes componentes del dominio</description></item>
///   <item><description>Define un contrato público para las operaciones de usuario</description></item>
/// </list>
///
/// <para><b>Patrón Result (CSharpFunctionalExtensions):</b></para>
/// <list type="bullet">
///   <item><description><c>Result&lt;T, E&gt;</c>: Representa una operación que puede tener éxito (T) o fallar (E)</description></item>
///   <item><description>Permite encadenar operaciones con métodos funcionales (Map, Bind, Tap)</description></item>
///   <item><description>Elimina necesidad de excepciones para control de flujo normal</description></item>
///   <item><description>Los errores de dominio están strongly-typed como <c>DomainError</c></description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>Manejo de Errores de Dominio:</b></para>
/// <list type="bullet">
///   <item><description>Los errores se representan mediante <c>DomainError</c> con propiedades: <c>Code</c>, <c>Message</c>, <c>Details</c>, <c>StatusCode</c></item>
///   <item><description>Códigos de error típicos: NotFound, Validation, Conflict, Forbidden, Unauthorized</description></item>
///   <item><description>El campo <c>StatusCode</c> permite mapear directamente a códigos HTTP</description></item>
/// </list>
/// <para><b>Seguridad y Validaciones:</b></para>
/// <list type="bullet">
///   <item><description>El email debe ser único y válido</description></item>
///   <item><description>El username debe ser único</description></item>
///   <item><description>La contraseña debe cumplir requisitos mínimos de seguridad</description></item>
///   <item><description>No se eliminan usuarios con pedidos asociados (soft delete)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Uso básico del patrón Result
/// [HttpGet("{id}")]
/// public async Task&lt;ActionResult&lt;UserDto&gt;&gt; GetUser(long id)
/// {
///     var result = await _userService.FindByIdAsync(id);
///     return result.Match(
///         user =&gt; Ok(user),
///         error =&gt; NotFound(new { error = error.Message })
///     );
/// }
///
/// // Encadenamiento de operaciones
/// public async Task&lt;Result&lt;UserDto, DomainError&gt;&gt; CrearYActivar(RegisterDto dto)
/// {
///     return await _userService.CreateAsync(dto)
///         .Tap(user =&gt; _emailService.EnviarBienvenida(user.Email))
///         .Map(user =&gt; user);
/// }
///
/// // Manejo de errores por código
/// if (result.IsFailure)
/// {
///     return result.Error.Code switch
///     {
///         ErrorCodes.NotFound =&gt; NotFound(),
///         ErrorCodes.Conflict =&gt; Conflict("Email ya registrado"),
///         ErrorCodes.Validation =&gt; BadRequest(result.Error.Message),
///         _ =&gt; StatusCode(500, "Error interno")
///     };
/// }
/// </code>
public interface IUserService
{
    /// <summary>
    /// Recupera todos los usuarios activos del sistema.
    /// Excluye usuarios eliminados lógicamente (soft delete).
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Enumerable con todos los usuarios activos</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca ocurre - siempre retorna lista</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Para grandes volúmenes, usar <see cref="FindAllPagedAsync(UserFilterDto)"/> en su lugar.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _userService.FindAllAsync();
    /// var usuarios = resultado.Value;
    /// return Ok(usuarios);
    /// </code>
    /// </example>
    Task<CFE.Result<IEnumerable<UserDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene usuarios de forma paginada con soporte para filtros.
    /// Permite buscar por email, username, rol y estado.
    /// </summary>
    /// <param name="filter">Criterios de filtrado, paginación y ordenamiento</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description><c>PagedResult</c> con usuarios y metadatos</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca con filtros válidos</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// var filter = new UserFilterDto
    /// {
    ///     Search = "juan",
    ///     Role = "Admin",
    ///     Activo = true,
    ///     Page = 1,
    ///     Size = 10
    /// };
    ///
    /// var resultado = await _userService.FindAllPagedAsync(filter);
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<CFE.Result<PagedResult<UserDto>, DomainError>> FindAllPagedAsync(UserFilterDto filter);

    /// <summary>
    /// Busca un usuario por su identificador único.
    /// </summary>
    /// <param name="id">ID numérico del usuario</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Datos completos del usuario</description></item>
    ///   <item><term><c>Result.Failure</c></term><description><c>DomainError</c> con código NotFound si no existe</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// var resultado = await _userService.FindByIdAsync(5);
    ///
    /// if (resultado.IsFailure)
    ///     return NotFound();
    ///
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<CFE.Result<UserDto, DomainError>> FindByIdAsync(long id);

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// Valida email único, username único, contraseña segura y crea el registro.
    /// </summary>
    /// <param name="dto">Datos de registro del nuevo usuario</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Usuario creado con datos de respuesta</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Validation (datos inválidos), Conflict (email/username existe)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>Email válido y único</description></item>
    ///   <item><description>Username único (3-50 caracteres alfanuméricos)</description></item>
    ///   <item><description>Contraseña: mínimo 8 caracteres, 1 mayúscula, 1 número</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var registro = new RegisterDto
    /// {
    ///     Email = "juan@email.com",
    ///     Username = "juan123",
    ///     Password = "Juan1234",
    ///     Nombre = "Juan Pérez"
    /// };
    ///
    /// var resultado = await _userService.CreateAsync(registro);
    /// return resultado.Match(
    ///     user =&gt; CreatedAtAction(nameof(GetUser), new { id = user.Id }, user),
    ///     error =&gt; BadRequest(new { code = error.Code, message = error.Message })
    /// );
    /// </code>
    /// </example>
    Task<CFE.Result<UserDto, DomainError>> CreateAsync(RegisterDto dto);

    /// <summary>
    /// Actualiza los datos de un usuario existente.
    /// Permite modificar email, username, nombre y otros campos.
    /// </summary>
    /// <param name="id">ID del usuario</param>
    /// <param name="dto">Nuevos datos del usuario</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Usuario actualizado</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound, Validation, o Conflict de email/username</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Si email o username cambian, se verifica unicidad. No se puede cambiar el rol del propio usuario.
    /// </remarks>
    /// <example>
    /// <code>
    /// var update = new UserUpdateDto
    /// {
    ///     Nombre = "Juan Pérez García",
    ///     Email = "juan.nuevo@email.com"
    /// };
    ///
    /// var resultado = await _userService.UpdateAsync(5, update);
    /// return resultado.Match(Ok, error =&gt; BadRequest(error.Message));
    /// </code>
    /// </example>
    Task<CFE.Result<UserDto, DomainError>> UpdateAsync(long id, UserUpdateDto dto);

    /// <summary>
    /// Actualiza la URL del avatar de un usuario.
    /// </summary>
    /// <param name="id">ID del usuario</param>
    /// <param name="avatarUrl">URL pública de la nueva imagen de avatar</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Usuario con nuevo avatar</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound o URL inválida</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// var resultado = await _userService.UpdateAvatarAsync(5, "https://cdn.example.com/avatars/juan.jpg");
    /// return resultado.Match(Ok, error =&gt; BadRequest(error.Message));
    /// </code>
    /// </example>
    Task<CFE.Result<UserDto, DomainError>> UpdateAvatarAsync(long id, string avatarUrl);

    /// <summary>
    /// Elimina un usuario del sistema (soft delete).
    /// El usuario se marca como eliminado y no puede autenticarse ni aparecer en búsquedas.
    /// </summary>
    /// <param name="id">ID del usuario a eliminar</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>UnitResult.Success</c></term><description>Usuario eliminado correctamente</description></item>
    ///   <item><term><c>UnitResult.Failure</c></term><description>NotFound o BusinessRuleViolation (tiene pedidos)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// No se elimina físicamente. Si el usuario tiene pedidos activos,
    /// se retorna error BusinessRuleViolation con detalles.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _userService.DeleteAsync(5);
    ///
    /// if (resultado.IsFailure)
    /// {
    ///     if (resultado.Error.Code == "USER_HAS_ORDERS")
    ///         return BadRequest("No se puede eliminar: el usuario tiene pedidos");
    ///     return NotFound();
    /// }
    ///
    /// return NoContent();
    /// </code>
    /// </example>
    Task<CFE.UnitResult<DomainError>> DeleteAsync(long id);
}
