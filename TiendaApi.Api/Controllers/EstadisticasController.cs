using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Estadisticas;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Extensions;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Estadisticas;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de estadísticas de la tienda.
///
/// 🎓 Las nueve vistas se calculan al vuelo sobre los datos existentes: no hay
/// tablas de agregados ni trabajos de fondo. Son solo lectura y están reservadas
/// al rol ADMIN porque incluyen cifras de negocio y datos de clientes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = UserRoles.ADMIN)]
public class EstadisticasController(
    IEstadisticasService service,
    ILogger<EstadisticasController> logger
) : ControllerBase
{
    /// <summary>
    /// Resumen general: ventas, pedidos por estado, inventario, usuarios y
    /// comparativa con el periodo anterior de igual duración.
    /// </summary>
    /// <param name="desde">Fecha inicial inclusive (ISO 8601). Opcional: histórico completo.</param>
    /// <param name="hasta">Fecha final exclusiva (ISO 8601). Opcional: hasta hoy.</param>
    /// <returns>200 OK con el resumen.</returns>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenEstadisticasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResumenEstadisticasDto>> Resumen(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        logger.LogInformation("Calculando resumen estadístico");

        var resultado = await service.ResumenAsync(desde, hasta);

        return resultado.Match(
            onSuccess: resumen => resumen,
            onFailure: error => error.ToHttpResult<ResumenEstadisticasDto>()
        );
    }

    /// <summary>
    /// Serie temporal de ventas con los periodos vacíos rellenos a cero
    /// (lista completa para gráficas sin huecos).
    /// </summary>
    /// <param name="desde">Fecha inicial inclusive (ISO 8601). Opcional.</param>
    /// <param name="hasta">Fecha final exclusiva (ISO 8601). Opcional.</param>
    /// <param name="agrupar">Agrupación: <c>dia</c>, <c>semana</c> (ISO 8601) o <c>mes</c>.</param>
    /// <returns>200 OK con la serie y los totales.</returns>
    [HttpGet("ventas")]
    [ProducesResponseType(typeof(VentasSerieDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VentasSerieDto>> Ventas(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string agrupar = "mes")
    {
        logger.LogInformation("Calculando serie de ventas ({Agrupar})", agrupar);

        var resultado = await service.VentasAsync(desde, hasta, agrupar);

        return resultado.Match(
            onSuccess: serie => serie,
            onFailure: error => error.ToHttpResult<VentasSerieDto>()
        );
    }

    /// <summary>
    /// Ranking de productos vendidos. El nombre de cada fila sale del propio pedido
    /// (dato histórico), así que un producto borrado sigue apareciendo.
    /// </summary>
    /// <param name="desde">Fecha inicial inclusive (ISO 8601). Opcional.</param>
    /// <param name="hasta">Fecha final exclusiva (ISO 8601). Opcional.</param>
    /// <param name="orden">Criterio: <c>unidades</c> (defecto) o <c>recaudacion</c>.</param>
    /// <param name="top">Número de filas, entre 1 y 50 (defecto 10).</param>
    /// <returns>200 OK con el ranking.</returns>
    [HttpGet("productos-mas-vendidos")]
    [ProducesResponseType(typeof(ProductosMasVendidosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductosMasVendidosDto>> ProductosMasVendidos(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string orden = "unidades",
        [FromQuery] int top = 10)
    {
        logger.LogInformation("Calculando ranking de productos ({Orden}, top {Top})", orden, top);

        var resultado = await service.ProductosMasVendidosAsync(desde, hasta, orden, top);

        return resultado.Match(
            onSuccess: ranking => ranking,
            onFailure: error => error.ToHttpResult<ProductosMasVendidosDto>()
        );
    }

    /// <summary>
    /// Rendimiento por categoría. Las categorías sin ventas aparecen con ceros,
    /// para que las gráficas muestren el catálogo completo.
    /// </summary>
    /// <param name="desde">Fecha inicial inclusive (ISO 8601). Opcional.</param>
    /// <param name="hasta">Fecha final exclusiva (ISO 8601). Opcional.</param>
    /// <returns>200 OK con el desglose.</returns>
    [HttpGet("categorias")]
    [ProducesResponseType(typeof(CategoriasEstadisticasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoriasEstadisticasDto>> Categorias(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        logger.LogInformation("Calculando estadísticas por categoría");

        var resultado = await service.CategoriasAsync(desde, hasta);

        return resultado.Match(
            onSuccess: desglose => desglose,
            onFailure: error => error.ToHttpResult<CategoriasEstadisticasDto>()
        );
    }

    /// <summary>
    /// Rendimiento por proveedor. Los productos sin proveedor se agrupan aparte
    /// con el código vacío, para no perder ventas en el recuento.
    /// </summary>
    /// <param name="desde">Fecha inicial inclusive (ISO 8601). Opcional.</param>
    /// <param name="hasta">Fecha final exclusiva (ISO 8601). Opcional.</param>
    /// <returns>200 OK con el desglose.</returns>
    [HttpGet("proveedores")]
    [ProducesResponseType(typeof(ProveedoresEstadisticasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProveedoresEstadisticasDto>> Proveedores(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        logger.LogInformation("Calculando estadísticas por proveedor");

        var resultado = await service.ProveedoresAsync(desde, hasta);

        return resultado.Match(
            onSuccess: desglose => desglose,
            onFailure: error => error.ToHttpResult<ProveedoresEstadisticasDto>()
        );
    }

    /// <summary>
    /// Embudo de estados. Los cancelados se reportan aparte y nunca suman
    /// en la recaudación del resto.
    /// </summary>
    /// <param name="desde">Fecha inicial inclusive (ISO 8601). Opcional.</param>
    /// <param name="hasta">Fecha final exclusiva (ISO 8601). Opcional.</param>
    /// <returns>200 OK con el embudo.</returns>
    [HttpGet("estados")]
    [ProducesResponseType(typeof(EstadosEstadisticasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EstadosEstadisticasDto>> Estados(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        logger.LogInformation("Calculando embudo de estados");

        var resultado = await service.EstadosAsync(desde, hasta);

        return resultado.Match(
            onSuccess: embudo => embudo,
            onFailure: error => error.ToHttpResult<EstadosEstadisticasDto>()
        );
    }

    /// <summary>
    /// Clientes con más gasto en la ventana, con su producto favorito.
    /// </summary>
    /// <param name="desde">Fecha inicial inclusive (ISO 8601). Opcional.</param>
    /// <param name="hasta">Fecha final exclusiva (ISO 8601). Opcional.</param>
    /// <param name="top">Número de filas, entre 1 y 50 (defecto 10).</param>
    /// <returns>200 OK con el ranking de clientes.</returns>
    [HttpGet("clientes")]
    [ProducesResponseType(typeof(ClientesEstadisticasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClientesEstadisticasDto>> Clientes(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int top = 10)
    {
        logger.LogInformation("Calculando ranking de clientes (top {Top})", top);

        var resultado = await service.ClientesAsync(desde, hasta, top);

        return resultado.Match(
            onSuccess: ranking => ranking,
            onFailure: error => error.ToHttpResult<ClientesEstadisticasDto>()
        );
    }

    /// <summary>
    /// Estado del inventario: valor del stock, agotados y productos con poco stock.
    /// Es una foto del catálogo actual, no depende de la ventana temporal.
    /// </summary>
    /// <param name="stockMax">Umbral de "stock bajo" (defecto 5).</param>
    /// <returns>200 OK con el inventario.</returns>
    [HttpGet("inventario")]
    [ProducesResponseType(typeof(InventarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InventarioDto>> Inventario([FromQuery] int stockMax = 5)
    {
        logger.LogInformation("Calculando inventario (umbral {StockMax})", stockMax);

        var resultado = await service.InventarioAsync(stockMax);

        return resultado.Match(
            onSuccess: inventario => inventario,
            onFailure: error => error.ToHttpResult<InventarioDto>()
        );
    }

    /// <summary>
    /// Evolución del precio de un producto leída del histórico de Cassandra:
    /// mínimo, máximo, media, último valor y variación porcentual.
    /// </summary>
    /// <param name="productoId">ID del producto.</param>
    /// <param name="desde">Fecha inicial del histórico (ISO 8601). Opcional.</param>
    /// <param name="hasta">Fecha final del histórico (ISO 8601). Opcional.</param>
    /// <returns>200 OK con la serie, o 404 si el producto no existe.</returns>
    [HttpGet("precios")]
    [ProducesResponseType(typeof(PreciosEstadisticasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreciosEstadisticasDto>> Precios(
        [FromQuery] long productoId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        logger.LogInformation("Calculando histórico de precios del producto {ProductoId}", productoId);

        var resultado = await service.PreciosAsync(productoId, desde, hasta);

        return resultado.Match(
            onSuccess: serie => serie,
            onFailure: error => error.ToHttpResult<PreciosEstadisticasDto>()
        );
    }
}
