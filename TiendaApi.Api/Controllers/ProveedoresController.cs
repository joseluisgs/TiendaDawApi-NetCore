using Asp.Versioning;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Extensions;
using TiendaApi.Api.Helpers.Pagination;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Proveedores;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para gestión de proveedores de productos.
/// Endpoints: CRUD paginado de proveedores.
///
/// A diferencia de categorías, estos endpoints son privados también en lectura: los proveedores
/// son datos internos de gestión y la lista completa no se expone al público. Por lo mismo no
/// se cachean las respuestas.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(Roles = UserRoles.ADMIN)]
public class ProveedoresController(
    IProveedorService service,
    ILogger<ProveedoresController> logger
) : ControllerBase
{
    /// <summary>
    /// Obtiene todos los proveedores paginados con filtros opcionales.
    /// </summary>
    /// <param name="nombre">Filtrar por nombre (contiene).</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación.</param>
    /// <param name="page">Número de página (0-indexed).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="sortBy">Campo de ordenación: codigo, nombre o createdAt.</param>
    /// <param name="direction">Dirección (asc, desc).</param>
    /// <returns>200 OK con lista paginada de proveedores.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProveedorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ProveedorDto>>> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "codigo",
        [FromQuery] string direction = "asc")
    {
        logger.LogInformation("Obteniendo proveedores paginados - Página: {Page}, Tamaño: {Size}", page, size);

        var filter = new ProveedorFilterDto
        {
            Nombre = nombre,
            IsDeleted = isDeleted,
            Page = page,
            Size = size,
            SortBy = sortBy,
            Direction = direction
        };

        var resultado = await service.FindAllPagedAsync(filter);

        return resultado.Match(
            onSuccess: proveedores =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(proveedores, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return proveedores;
            },
            onFailure: error => error.ToHttpResult<PagedResult<ProveedorDto>>()
        );
    }

    /// <summary>
    /// Obtiene un proveedor por su código.
    /// </summary>
    /// <param name="codigo">Código de 11 caracteres estilo YouTube.</param>
    /// <returns>200 OK con el proveedor, o 404 si no existe.</returns>
    [HttpGet("{codigo}")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProveedorDto>> GetById(string codigo)
    {
        logger.LogInformation("Obteniendo proveedor con código: {Codigo}", codigo);

        var resultado = await service.FindByIdAsync(codigo);

        return resultado.Match(
            onSuccess: proveedor => proveedor,
            onFailure: error => error.ToHttpResult<ProveedorDto>()
        );
    }

    /// <summary>
    /// Crea un nuevo proveedor.
    /// Si no se envía código, el servidor genera un identificador de 11 caracteres.
    /// </summary>
    /// <param name="dto">Datos del proveedor a crear.</param>
    /// <returns>201 Created con el proveedor creado, o 400/409 si hay errores.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDto>> Create([FromBody] ProveedorRequestDto dto)
    {
        logger.LogInformation("Creando nuevo proveedor: {Nombre}", dto.Nombre);

        var resultado = await service.CreateAsync(dto);

        return resultado.Match(
            onSuccess: proveedor => CreatedAtAction(nameof(GetById), new { codigo = proveedor.Codigo }, proveedor),
            onFailure: error => error.ToHttpResult<ProveedorDto>()
        );
    }

    /// <summary>
    /// Actualiza los datos editables de un proveedor.
    /// El código es la clave del registro y no se modifica.
    /// </summary>
    /// <param name="codigo">Código del proveedor a actualizar.</param>
    /// <param name="dto">Nuevos datos del proveedor.</param>
    /// <returns>200 OK con el proveedor actualizado, o 400/404/409 si hay errores.</returns>
    [HttpPut("{codigo}")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDto>> Update(string codigo, [FromBody] ProveedorRequestDto dto)
    {
        logger.LogInformation("Actualizando proveedor con código: {Codigo}", codigo);

        var resultado = await service.UpdateAsync(codigo, dto);

        return resultado.Match(
            onSuccess: proveedor => proveedor,
            onFailure: error => error.ToHttpResult<ProveedorDto>()
        );
    }

    /// <summary>
    /// Elimina un proveedor (soft-delete).
    /// Se rechaza la operación si el proveedor todavía tiene productos asociados.
    /// </summary>
    /// <param name="codigo">Código del proveedor a eliminar.</param>
    /// <returns>204 No Content si tiene éxito, o 404/422 si no existe o tiene productos.</returns>
    [HttpDelete("{codigo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Delete(string codigo)
    {
        logger.LogInformation("Eliminando proveedor con código: {Codigo}", codigo);

        var resultado = await service.DeleteAsync(codigo);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ForbiddenError => StatusCode(StatusCodes.Status403Forbidden, new { message = error.Message }),
            BusinessRuleError => StatusCode(StatusCodes.Status422UnprocessableEntity, new { message = error.Message }),
            ValidationError => StatusCode(StatusCodes.Status400BadRequest, new { message = error.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = error.Message })
        };
    }
}
