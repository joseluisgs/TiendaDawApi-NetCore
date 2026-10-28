namespace TiendaApi.Api.Dtos.Historial;

// DTOs de lectura de las series de Cassandra. Se mapean a `double` (no decimal)
// porque CQL decimal se mapa a Cassandra.BigDecimal del driver, que no es
// System.Decimal: con double el mapeo es directo (misma decision del ejemplo 26).

/// <summary>
/// Punto del histórico de precios de un producto (consulta Q-A de Cassandra).
/// </summary>
/// <param name="Fecha">Momento del cambio (UTC). El orden lo da la clustering key de la tabla.</param>
/// <param name="Precio">Precio del producto en ese momento.</param>
public record HistorialPrecioDto(DateTime Fecha, double Precio);

/// <summary>
/// Entrada del feed de actividad: un producto visto por un usuario (consulta Q-B).
/// El nombre viene DENORMALIZADO de la tabla: el feed se pinta sin ningún cruce.
/// </summary>
/// <param name="Fecha">Momento de la vista (UTC).</param>
/// <param name="ProductoId">Producto visto.</param>
/// <param name="Nombre">Nombre del producto copiado en la fila (sin joins en Cassandra).</param>
public record VistaFeedDto(DateTime Fecha, long ProductoId, string Nombre);

/// <summary>
/// Cambio de precio registrado en el informe por día (consulta Q-C).
/// </summary>
/// <param name="Fecha">Día del cambio (bucket de la partición).</param>
/// <param name="ProductoId">Producto que cambió de precio.</param>
/// <param name="Precio">Precio nuevo.</param>
public record CambioPrecioDto(DateOnly Fecha, long ProductoId, double Precio);
