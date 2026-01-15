using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Services.Pedidos;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador REST para la gestión de pedidos.
/// Implementa el patrón de diseño Result para el manejo de operaciones y errores.
/// </summary>
/// <remarks>
/// <para><b>API REST:</b> Este controlador expone endpoints que siguen los principios de RESTful.</para>
/// <para><b>Métodos HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>GET</term>
/// <description>Recuperar recursos (pedidos)</description>
/// </item>
/// <item>
/// <term>POST</term>
/// <description>Crear nuevos recursos</description>
/// </item>
/// <item>
/// <term>PUT</term>
/// <description>Actualizar recursos existentes</description>
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
/// <para>Todos los endpoints requieren autenticación. Los administradores pueden acceder a todos los pedidos. Los usuarios pueden acceder únicamente a sus propios pedidos.</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController(
    IPedidosService service
) : ControllerBase
{

    /// <summary>
    /// Obtiene todos los pedidos del sistema.
    /// </summary>
    /// <returns>Lista de todos los pedidos.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/pedidos</para>
    /// <para><b>Descripción:</b> Retorna una lista con todos los pedidos del sistema. Solo accesible por administradores.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Lista de pedidos retornada exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// [
    ///   {
    ///     "id": "PED-001",
    ///     "userId": 1,
    ///     "userName": "Juan Pérez",
    ///     "estado": "Pendiente",
    ///     "total": 299.99,
    ///     "detalles": [...],
    ///     "fechaCreacion": "2024-01-15T10:30:00Z"
    ///   }
    /// ]
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/pedidos" \
    ///   -H "Authorization: Bearer {admin_token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPedidos()
    {
        var resultado = await service.FindAllAsync();

        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Crea un nuevo pedido para el usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos del pedido a crear.</param>
    /// <returns>Los datos del pedido creado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> POST /api/pedidos</para>
    /// <para><b>Descripción:</b> Registra un nuevo pedido asociado al usuario autenticado. El pedido se crea con estado inicial "Pendiente".</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (cualquier usuario autenticado).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>201 Created</term><description>Pedido creado exitosamente. Incluye Location header.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos, errores de validación, o productos no disponibles.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>404 Not Found</term><description>Uno o más productos del pedido no existen.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "detalles": [
    ///     {
    ///       "productoId": 1,
    ///       "cantidad": 2
    ///     },
    ///     {
    ///       "productoId": 3,
    ///       "cantidad": 1
    ///     }
    ///   ],
    ///   "direccionEnvio": "Calle Principal 123, Ciudad",
    ///   "observaciones": "Entregar en horario de mañana"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X POST "http://localhost:5000/api/pedidos" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"detalles": [{"productoId": 1, "cantidad": 2}], "direccionEnvio": "Calle Principal 123"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePedido([FromBody] PedidoRequestDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.CreateAsync(userId, dto);

        if (resultado.IsSuccess)
        {
            var pedido = resultado.Value;
            return CreatedAtAction(nameof(GetPedidoById), new { id = pedido.Id }, pedido);
        }

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
            BusinessRuleError => BadRequest(new { message = error.Message }),
            UnauthorizedError => Unauthorized(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            ConflictError => Conflict(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Obtiene los pedidos del usuario autenticado.
    /// </summary>
    /// <returns>Lista de pedidos del usuario autenticado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/pedidos/me</para>
    /// <para><b>Descripción:</b> Retorna todos los pedidos asociados al usuario actualmente autenticado.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (cualquier usuario autenticado).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Lista de pedidos del usuario retornada exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// [
    ///   {
    ///     "id": "PED-001",
    ///     "estado": "Entregado",
    ///     "total": 299.99,
    ///     "detalles": [...],
    ///     "fechaCreacion": "2024-01-15T10:30:00Z"
    ///   }
    /// ]
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/pedidos/me" \
    ///   -H "Authorization: Bearer {token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidos()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindByUserIdAsync(userId);

        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene un pedido específico por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del pedido.</param>
    /// <returns>Los datos del pedido encontrado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/pedidos/{id}</para>
    /// <para><b>Descripción:</b> Busca y retorna un pedido específico. Los usuarios solo pueden ver sus propios pedidos; los administradores pueden ver todos.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (el usuario debe ser propietario del pedido o administrador).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Pedido encontrado exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario no tiene permiso para ver este pedido.</description></item>
    /// <item><term>404 Not Found</term><description>No existe pedido con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "id": "PED-001",
    ///   "userId": 1,
    ///   "userName": "Juan Pérez",
    ///   "estado": "Pendiente",
    ///   "total": 299.99,
    ///   "direccionEnvio": "Calle Principal 123",
    ///   "detalles": [
    ///     {
    ///       "productoId": 1,
    ///       "productoNombre": "Laptop HP",
    ///       "cantidad": 1,
    ///       "precioUnitario": 299.99
    ///     }
    ///   ],
    ///   "fechaCreacion": "2024-01-15T10:30:00Z"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/pedidos/PED-001" \
    ///   -H "Authorization: Bearer {token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPedidoById(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindByIdAsync(id);

        if (resultado.IsFailure)
        {
            var error = resultado.Error;
            return error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            };
        }

        var pedido = resultado.Value;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        if (pedido.UserId != userId && userRole != UserRoles.ADMIN)
            return Forbid();

        return Ok(pedido);
    }

    /// <summary>
    /// Actualiza el estado de un pedido.
    /// </summary>
    /// <param name="id">Identificador único del pedido.</param>
    /// <param name="dto">Nuevo estado para el pedido.</param>
    /// <returns>Los datos del pedido actualizado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PUT /api/pedidos/{id}/estado</para>
    /// <para><b>Descripción:</b> Actualiza el estado de un pedido. Solo accesible por administradores.</para>
    /// <para><b>Estados posibles:</b> Pendiente, Procesando, Enviado, Entregado, Cancelado</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Estado del pedido actualizado exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Transición de estado no válida.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe pedido con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "estado": "Enviado"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PUT "http://localhost:5000/api/pedidos/PED-001/estado" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {admin_token}" \
    ///   -d '{"estado": "Enviado"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPut("{id}/estado")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedidoEstado(string id, [FromBody] UpdateEstadoDto dto)
    {
        var resultado = await service.UpdateEstadoAsync(id, dto.Estado);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                UnauthorizedError => Unauthorized(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza un pedido existente.
    /// </summary>
    /// <param name="id">Identificador único del pedido.</param>
    /// <param name="dto">Nuevos datos para el pedido.</param>
    /// <returns>Los datos del pedido actualizado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PUT /api/pedidos/{id}</para>
    /// <para><b>Descripción:</b> Actualiza los datos de un pedido. Los usuarios pueden actualizar sus propios pedidos (solo en ciertos estados).</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (el usuario debe ser propietario del pedido o administrador).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Pedido actualizado exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario no tiene permiso para actualizar este pedido.</description></item>
    /// <item><term>404 Not Found</term><description>No existe pedido con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "direccionEnvio": "Nueva Calle 456",
    ///   "observaciones": "Actualizar dirección de entrega"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PUT "http://localhost:5000/api/pedidos/PED-001" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"direccionEnvio": "Nueva Calle 456", "observaciones": "Actualizar dirección"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedido(string id, [FromBody] UpdatePedidoDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.UpdateAsync(id, userId, dto);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                UnauthorizedError => Unauthorized(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina un pedido del sistema.
    /// </summary>
    /// <param name="id">Identificador único del pedido.</param>
    /// <returns>Sin contenido en caso de éxito.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> DELETE /api/pedidos/{id}</para>
    /// <para><b>Descripción:</b> Elimina un pedido del sistema. Los usuarios pueden eliminar sus propios pedidos (solo si están en estado "Pendiente").</para>
    /// <para><b>Autenticación:</b> Requiere JWT token (el usuario debe ser propietario del pedido o administrador).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>204 No Content</term><description>Pedido eliminado exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario no tiene permiso para eliminar este pedido.</description></item>
    /// <item><term>404 Not Found</term><description>No existe pedido con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X DELETE "http://localhost:5000/api/pedidos/PED-001" \
    ///   -H "Authorization: Bearer {token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePedido(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.DeleteAsync(id, userId);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}
