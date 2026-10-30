using Cassandra;
using Cassandra.Data.Linq;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Models;
using ISession = Cassandra.ISession;

namespace TiendaApi.Api.Repositories.Historial;

/// <inheritdoc />
/// <remarks>
/// 🎓 Query-first: cada método corresponde a una consulta de la pizarra (Q-A/Q-B/Q-C)
/// y a la tabla diseñada para responderla. Dos estilos de mapeo conviven a propósito:
/// LINQ <c>Table&lt;T&gt;</c> (atributos) para columnas timestamp, y
/// <c>SimpleStatement</c> manual para la columna CQL <c>date</c>, que el driver
/// 3.23 no mapea desde <c>DateTime</c>/<c>DateOnly</c> ("Unknown Cassandra target
/// type"): para <c>date</c> el tipo del driver es <c>Cassandra.LocalDate</c>.
/// </remarks>
public class CassandraHistorialRepository(ISession session) : IHistorialRepository
{
    // El session ya está en el keyspace (ChangeKeyspace en InitializeSchemaAsync).
    private readonly Table<HistorialPrecioPorProducto> _historial = new(session);
    private readonly Table<VistaPorUsuario> _vistas = new(session);

    /// <inheritdoc />
    public async Task<IReadOnlyList<HistorialPrecioDto>> GetHistorialPreciosAsync(
        long productoId, DateTime? desde, DateTime? hasta, int limit)
    {
        // SELECT * FROM historial_precios_por_producto WHERE producto_id = ?
        //   [AND fecha >= ? AND fecha <= ?]   <- rango sobre CLUSTERING key: permitido
        //   LIMIT ?                           <- sin ORDER BY: el DESC lo guarda la tabla
        var query = _historial.Where(h => h.ProductoId == productoId);

        if (desde.HasValue)
            query = query.Where(h => h.Fecha >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(h => h.Fecha <= hasta.Value);

        var filas = await query.Take(limit).ExecuteAsync();

        return filas
            .Select(h => new HistorialPrecioDto(ComoUtc(h.Fecha), h.Precio))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VistaFeedDto>> GetFeedVistasAsync(long usuarioId, int limit)
    {
        // SELECT * FROM vistas_por_usuario WHERE usuario_id = ? LIMIT ?
        // Orden DESC de serie (clustering) y TTL de 180 días aplicado por la tabla.
        var filas = await _vistas
            .Where(v => v.UsuarioId == usuarioId)
            .Take(limit)
            .ExecuteAsync();

        return filas
            .Select(v => new VistaFeedDto(ComoUtc(v.Fecha), v.ProductoId, v.Nombre))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CambioPrecioDto>> GetCambiosPrecioAsync(DateOnly desde, DateOnly hasta)
    {
        // 🎓 Un rango (>=, <=) sobre la PARTITION key está prohibido en CQL:
        // solo caben igualdades e IN. "Los cambios de esta semana" se resuelve
        // como IN con los días concretos = lookup puntual de N particiones.
        // Se acota a 31 días: más particiones por consulta no tiene sentido.
        var dias = hasta.DayNumber - desde.DayNumber + 1;
        if (dias > 31)
            throw new ArgumentOutOfRangeException(nameof(hasta),
                "El rango no puede superar 31 días (se resolvería como IN de 31 particiones)");

        // DateOnly solo genera dígitos y guiones con el formato fijo: no hay
        // forma de inyectar CQL a través de este valor (además es un struct).
        var inDias = string.Join(", ",
            Enumerable.Range(0, dias)
                .Select(i => $"'{desde.AddDays(i):yyyy-MM-dd}'"));

        // SELECT fecha, producto_id, precio FROM cambios_precio_por_fecha
        //   WHERE fecha IN (...)
        using var resultado = await session.ExecuteAsync(new SimpleStatement(
            $"SELECT fecha, producto_id, precio FROM cambios_precio_por_fecha WHERE fecha IN ({inDias})"));

        var cambios = new List<CambioPrecioDto>();
        foreach (var fila in resultado)
        {
            cambios.Add(new CambioPrecioDto(
                DateOnlyFrom(fila.GetValue<LocalDate>("fecha")),
                fila.GetValue<long>("producto_id"),
                fila.GetValue<double>("precio")));
        }

        // El IN no garantiza orden entre particiones: lo ordenamos en la app.
        return cambios
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.ProductoId)
            .ToList();
    }

    /// <inheritdoc />
    public async Task RegistrarPrecioAsync(long productoId, double precio)
    {
        var ahora = DateTime.UtcNow;

        // 1) Q-A: una fila más en el histórico del producto (append-only, sin updates).
        await _historial.Insert(new HistorialPrecioPorProducto
        {
            ProductoId = productoId,
            Fecha = ahora,
            Precio = precio
        }).ExecuteAsync();

        // 2) Q-C: el informe del día. Dos tablas, dos inserts SIN transacción
        // repartida: si la segunda escritura fallara, la primera quedaría —
        // Cassandra prioriza la escritura rápida sobre la atomicidad repartida.
        await session.ExecuteAsync(new SimpleStatement(
            "INSERT INTO cambios_precio_por_fecha (fecha, producto_id, precio) VALUES (?, ?, ?)",
            new LocalDate(ahora.Year, ahora.Month, ahora.Day),
            productoId,
            precio));
    }

    /// <inheritdoc />
    public async Task RegistrarVistaAsync(long usuarioId, long productoId, string nombre)
    {
        // El nombre se COPIA a la fila (denormalización: el feed no hará ningún
        // cruce) y el TTL lo aplica la tabla: sin USING TTL y sin job de purga.
        await _vistas.Insert(new VistaPorUsuario
        {
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            ProductoId = productoId,
            Nombre = nombre ?? string.Empty
        }).ExecuteAsync();
    }

    /// <summary>
    /// El driver devuelve los timestamps con Kind=Unspecified; los normalizamos a
    /// UTC para que la serialización JSON salga siempre con sufijo Z.
    /// </summary>
    private static DateTime ComoUtc(DateTime fecha) =>
        fecha.Kind == DateTimeKind.Utc ? fecha : DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

    /// <summary>
    /// Convierte el LocalDate del driver (columna CQL date) a System.DateOnly.
    /// </summary>
    private static DateOnly DateOnlyFrom(LocalDate fecha) =>
        new(fecha.Year, fecha.Month, fecha.Day);
}
