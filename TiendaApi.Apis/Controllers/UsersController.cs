using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Services.Storage;
using TiendaApi.Apis.Services.Users;
using TiendaApi.Apis.Utils.Pagination;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador REST para la gestión de usuarios y sus pedidos.
/// Implementa el patrón de diseño Result para el manejo de operaciones y errores.
/// </summary>
/// <remarks>
/// <para><b>API REST:</b> Este controlador expone endpoints que siguen los principios de RESTful.</para>
/// <para><b>Métodos HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>GET</term>
/// <description>Recuperar recursos (usuarios, perfiles, pedidos)</description>
/// </item>
/// <item>
/// <term>POST</term>
/// <description>Crear nuevos recursos (usuarios, pedidos)</description>
/// </item>
/// <item>
/// <term>PUT</term>
/// <description>Actualizar recursos existentes completamente</description>
/// </item>
/// <item>
/// <term>PATCH</term>
/// <description>Actualizar parcialmente recursos</description>
/// </item>
/// <item>
/// <term>DELETE</term>
/// <description>Eliminar recursos</description>
/// </item>
/// </list>
/// <para><b>Códigos de estado HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>200 OK</term>
/// <description>Petición exitosa, retorna datos</description>
/// </item>
/// <item>
/// <term>201 Created</term>
/// <description>Recurso creado exitosamente</description>
/// </item>
/// <item>
/// <term>204 No Content</term>
/// <description>Petición exitosa sin contenido que retornar</description>
/// </item>
/// <item>
/// <term>400 Bad Request</term>
/// <description>Error en los datos enviados por el cliente</description>
/// </item>
/// <item>
/// <term>401 Unauthorized</term>
/// <description>Usuario no autenticado</description>
/// </item>
/// <item>
/// <term>403 Forbidden</term>
/// <description>Usuario autenticado sin permisos suficientes</description>
/// </item>
/// <item>
/// <term>404 Not Found</term>
/// <description>Recurso no encontrado</description>
/// </item>
/// <item>
/// <term>409 Conflict</term>
/// <description>Conflicto con el estado actual del recurso</description>
/// </item>
/// <item>
/// <term>500 Internal Server Error</term>
/// <description>Error interno del servidor</description>
/// </item>
/// </list>
/// <para><b>Autorización:</b></para>
/// <para>Los endpoints bajo la ruta "me" son accesibles por cualquier usuario autenticado para gestionar su propia información. Los endpoints de administración requieren rol de Administrador.</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController(
    IUserService service,
    ILogger<UsersController> logger
) : ControllerBase
{

    /// <summary>
    /// Obtiene todos los usuarios de forma paginada con filtros opcionales.
    /// </summary>
    /// <param name="username">Filtrar por nombre de usuario (búsqueda parcial, opcional).</param>
    /// <param name="email">Filtrar por email (búsqueda exacta, opcional).</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación (opcional): true para eliminados, false para activos, null para todos.</param>
    /// <param name="page">Número de página (base 0). Por defecto: 0.</param>
    /// <param name="size">Cantidad de elementos por página. Por defecto: 10.</param>
    /// <param name="sortBy">Campo por el cual ordenar. Por defecto: "id".</param>
    /// <param name="direction">Dirección de ordenamiento: "asc" o "desc". Por defecto: "asc".</param>
    /// <returns>Resultado paginado con la lista de usuarios.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/users</para>
    /// <para><b>Descripción:</b> Retorna una lista paginada de todos los usuarios del sistema. Solo accesible por administradores.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Lista de usuarios retornada exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "items": [
    ///     {
    ///       "id": 1,
    ///       "username": "juanperez",
    ///       "email": "juan@example.com",
    ///       "role": "USER",
    ///       "isDeleted": false,
    ///       "createdAt": "2024-01-01T00:00:00Z"
    ///     }
    ///   ],
    ///   "page": 0,
    ///   "size": 10,
    ///   "totalItems": 50,
    ///   "totalPages": 5
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/users?username=juan&page=0&size=10" \
    ///   -H "Authorization: Bearer {admin_token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? username = null,
        [FromQuery] string? email = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] string direction = "asc")
    {
        logger.LogInformation("Obteniendo todos los usuarios - Página: {Page}, Tamaño: {Size}", page, size);

        var filter = new UserFilterDto(
            username,
            email,
            isDeleted,
            page,
            size,
            sortBy,
            direction
        );

        var resultado = await service.FindAllPagedAsync(filter);

        return resultado.Match(
            onSuccess: pagedResult =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(pagedResult, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(pagedResult);
            },
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene un usuario específico por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del usuario.</param>
    /// <returns>Los datos del usuario encontrado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/users/{id}</para>
    /// <para><b>Descripción:</b> Busca y retorna un usuario específico usando su ID. Solo accesible por administradores.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Usuario encontrado exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe usuario con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "id": 1,
    ///   "username": "juanperez",
    ///   "email": "juan@example.com",
    ///   "firstName": "Juan",
    ///   "lastName": "Pérez",
    ///   "role": "USER",
    ///   "isDeleted": false,
    ///   "createdAt": "2024-01-01T00:00:00Z"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/users/1" \
    ///   -H "Authorization: Bearer {admin_token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        logger.LogInformation("Obteniendo usuario con ID: {Id}", id);

        var resultado = await service.FindByIdAsync(id);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crea un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="dto">Datos del usuario a crear.</param>
    /// <returns>Los datos del usuario creado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> POST /api/users</para>
    /// <para><b>Descripción:</b> Registra un nuevo usuario en el sistema. Solo accesible por administradores.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>201 Created</term><description>Usuario creado exitosamente. Incluye Location header.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>409 Conflict</term><description>Ya existe un usuario con el mismo username o email.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "username": "mariagarcia",
    ///   "email": "maria@example.com",
    ///   "password": "Contraseña123!",
    ///   "firstName": "María",
    ///   "lastName": "García",
    ///   "role": "USER"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X POST "http://localhost:5000/api/users" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {admin_token}" \
    ///   -d '{"username": "mariagarcia", "email": "maria@example.com", "password": "Contraseña123!", "firstName": "María", "lastName": "García"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] RegisterDto dto)
    {
        logger.LogInformation("Creando nuevo usuario: {Username}", dto.Username);

        var resultado = await service.CreateAsync(dto);

        return resultado.Match(
            onSuccess: usuario => CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario),
            onFailure: error => error switch
            {
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza un usuario existente completamente.
    /// </summary>
    /// <param name="id">Identificador único del usuario a actualizar.</param>
    /// <param name="dto">Nuevos datos para el usuario.</param>
    /// <returns>Los datos del usuario actualizado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PUT /api/users/{id}</para>
    /// <para><b>Descripción:</b> Actualiza todos los campos de un usuario existente. Solo accesible por administradores.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Usuario actualizado exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe usuario con el ID especificado.</description></item>
    /// <item><term>409 Conflict</term><description>Conflicto con datos existentes (ej: email duplicado).</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "username": "mariagarcia_updated",
    ///   "email": "maria.nueva@example.com",
    ///   "firstName": "María José",
    ///   "lastName": "García López",
    ///   "role": "USER"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PUT "http://localhost:5000/api/users/2" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {admin_token}" \
    ///   -d '{"username": "mariagarcia_updated", "email": "maria.nueva@example.com", "firstName": "María José"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UserUpdateDto dto)
    {
        logger.LogInformation("Actualizando usuario con ID: {Id}", id);

        var resultado = await service.UpdateAsync(id, dto);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza el avatar de un usuario.
    /// </summary>
    /// <param name="id">Identificador único del usuario.</param>
    /// <param name="dto">URL del nuevo avatar.</param>
    /// <returns>Los datos del usuario con el avatar actualizado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PATCH /api/users/{id}/avatar</para>
    /// <para><b>Descripción:</b> Actualiza únicamente el avatar de un usuario. El usuario puede actualizar su propio avatar o un administrador puede actualizar cualquier avatar.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (el usuario debe ser propietario del perfil o administrador).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Avatar actualizado exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>URL de avatar inválida.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario no tiene permiso para actualizar este avatar.</description></item>
    /// <item><term>404 Not Found</term><description>No existe usuario con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "avatarUrl": "https://ejemplo.com/avatars/maria.jpg"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PATCH "http://localhost:5000/api/users/2/avatar" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"avatarUrl": "https://ejemplo.com/avatars/maria.jpg"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPatch("{id}/avatar")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvatar(long id, [FromBody] AvatarUpdateDto dto)
    {
        logger.LogInformation("Actualizando avatar de usuario con ID: {Id}", id);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.UpdateAvatarAsync(id, dto.AvatarUrl);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina un usuario del sistema.
    /// </summary>
    /// <param name="id">Identificador único del usuario a eliminar.</param>
    /// <returns>Sin contenido en caso de éxito.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> DELETE /api/users/{id}</para>
    /// <para><b>Descripción:</b> Elimina un usuario del sistema (eliminación lógica). Solo accesible por administradores.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>204 No Content</term><description>Usuario eliminado exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe usuario con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X DELETE "http://localhost:5000/api/users/2" \
    ///   -H "Authorization: Bearer {admin_token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        logger.LogInformation("Eliminando usuario con ID: {Id}", id);

        var resultado = await service.DeleteAsync(id);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Obtiene el perfil del usuario autenticado.
    /// </summary>
    /// <returns>Los datos del perfil del usuario autenticado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/users/me/profile</para>
    /// <para><b>Descripción:</b> Retorna los datos del perfil del usuario actualmente autenticado.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (cualquier usuario autenticado).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Perfil del usuario retornado exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "id": 1,
    ///   "username": "juanperez",
    ///   "email": "juan@example.com",
    ///   "firstName": "Juan",
    ///   "lastName": "Pérez",
    ///   "avatarUrl": "https://ejemplo.com/avatars/juan.jpg",
    ///   "role": "USER",
    ///   "createdAt": "2024-01-01T00:00:00Z"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/users/me/profile" \
    ///   -H "Authorization: Bearer {token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindByIdAsync(userId);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza el perfil del usuario autenticado.
    /// </summary>
    /// <param name="dto">Nuevos datos para el perfil.</param>
    /// <returns>Los datos del perfil actualizado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PUT /api/users/me/profile</para>
    /// <para><b>Descripción:</b> Actualiza los datos del perfil del usuario actualmente autenticado.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (cualquier usuario autenticado).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Perfil actualizado exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>404 Not Found</term><description>Usuario no encontrado (cuenta eliminada).</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "firstName": "Juan Carlos",
    ///   "lastName": "Pérez García",
    ///   "email": "juan.carlos@example.com"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PUT "http://localhost:5000/api/users/me/profile" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"firstName": "Juan Carlos", "lastName": "Pérez García"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPut("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UserUpdateDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        logger.LogInformation("Usuario {UserId} actualizando su perfil", userId);

        var resultado = await service.UpdateAsync(userId, dto);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina la cuenta del usuario autenticado.
    /// </summary>
    /// <returns>Sin contenido en caso de éxito.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> DELETE /api/users/me/profile</para>
    /// <para><b>Descripción:</b> Elimina la cuenta del usuario actualmente autenticado (eliminación lógica).</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (cualquier usuario autenticado).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>204 No Content</term><description>Cuenta eliminada exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// </list>
    /// <para><b>Nota:</b> Esta acción elimina lógicamente al usuario. Los datos asociados (como pedidos) se conservan.</para>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X DELETE "http://localhost:5000/api/users/me/profile" \
    ///   -H "Authorization: Bearer {token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpDelete("me/profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        logger.LogInformation("Usuario {UserId} eliminando su cuenta", userId);

        var resultado = await service.DeleteAsync(userId);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

}

/// <summary>
/// DTO para actualizar el avatar de un usuario.
/// </summary>
public record AvatarUpdateDto
{
    /// <summary>
    /// URL del nuevo avatar del usuario.
    /// </summary>
    /// <example>https://ejemplo.com/avatars/nuevo-avatar.jpg</example>
    public string AvatarUrl { get; init; } = string.Empty;
}
