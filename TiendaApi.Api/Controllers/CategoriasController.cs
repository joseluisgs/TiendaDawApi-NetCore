using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Helpers.Pagination;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para la gestión de categorías de productos.
/// Permite realizar operaciones CRUD con soporte para paginación y filtrado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController(
    ICategoriaService service,
    ILogger<CategoriasController> logger
) : ControllerBase
{
    /// <summary>
    /// Obtiene todas las categorías paginadas aplicando filtros opcionales.
    /// </summary>
    /// <param name="nombre">Filtrar por nombre de categoría (contiene).</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación lógica.</param>
    /// <param name="page">Número de página a recuperar (0-indexed).</param>
    /// <param name="size">Cantidad de elementos por página.</param>
    /// <param name="sortBy">Campo por el cual ordenar los resultados.</param>
    /// <param name="direction">Dirección de la ordenación (asc o desc).</param>
    /// <returns>Una respuesta 200 OK con el objeto <see cref="PagedResult{CategoriaDto}"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoriaDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] string direction = "asc")
    {
        logger.LogInformation("Obteniendo categorías paginadas - Página: {Page}, Tamaño: {Size}", page, size);

        var filter = new CategoriaFilterDto
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
            onSuccess: categorias =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(categorias, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(categorias);
            },
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Recupera una categoría específica mediante su identificador único.
    /// </summary>
    /// <param name="id">ID numérico de la categoría.</param>
    /// <returns>La categoría encontrada o 404 si no existe.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        logger.LogInformation("Obteniendo categoría con ID: {Id}", id);

        var resultado = await service.FindByIdAsync(id);

        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crea una nueva categoría de productos.
    /// Solo accesible para usuarios con rol de administrador.
    /// </summary>
    /// <param name="dto">Datos de la categoría a crear.</param>
    /// <returns>201 Created con la categoría recién creada.</returns>
    [HttpPost]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CategoriaRequestDto dto)
    {
        logger.LogInformation("Creando nueva categoría: {Nombre}", dto.Nombre);

        var resultado = await service.CreateAsync(dto);

        return resultado.Match(
            onSuccess: categoria => CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria),
            onFailure: error => error switch
            {
                ValidationError => BadRequest(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza los datos de una categoría existente.
    /// Requiere permisos de administrador.
    /// </summary>
    /// <param name="id">ID de la categoría a actualizar.</param>
    /// <param name="dto">Nuevos datos de la categoría.</param>
    /// <returns>200 OK con la categoría actualizada o error correspondiente.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] CategoriaRequestDto dto)
    {
        logger.LogInformation("Actualizando categoría con ID: {Id}", id);

        var resultado = await service.UpdateAsync(id, dto);

        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina una categoría del sistema (borrado lógico).
    /// Requiere permisos de administrador.
    /// </summary>
    /// <param name="id">ID de la categoría a eliminar.</param>
    /// <returns>204 No Content si se eliminó con éxito.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204 NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        logger.LogInformation("Eliminando categoría con ID: {Id}", id);

        var resultado = await service.DeleteAsync(id);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ValidationError => BadRequest(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}