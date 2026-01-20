using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Services.Categorias;
using TiendaApi.Apis.Helpers.Pagination;

namespace TiendaApi.Apis.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController(
    ICategoriaService service,
    ILogger<CategoriasController> logger
) : ControllerBase
{
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

    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
