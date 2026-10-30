using Cassandra;
using Cassandra.Data.Linq;
using TiendaApi.Api.Models;
using ISession = Cassandra.ISession;

namespace TiendaApi.Api.Data.Seed.Cassandra;

/// <summary>
/// Seeder de Cassandra: serie histórica de precios (Q-A) y feed de actividad (Q-B).
///
/// 🎓 Va DESPUÉS de <c>InitializeCassandraAsync</c>: necesita el esquema creado.
/// Como <c>MongoDbSeeder</c>, es idempotente (si la partición ya tiene filas lo
/// omite), y su trabajo vive en un try propio: son datos de ejemplo, no un
/// prerequisito del arranque de la API.
/// Ojo con el ciclo de vida en desarrollo: PostgreSQL se ELIMINA y recrea en cada
/// arranque, pero el keyspace NO (CREATE IF NOT EXISTS + datos persistentes);
/// por eso la comprobación de "¿ya sembrado?" es obligatoria, no opcional.
/// </summary>
public class CassandraSeeder(ISession session, ILogger<CassandraSeeder> logger)
{
    /// <summary>
    /// Siembra historial y feed si sus particiones están vacías: comprueba el
    /// estado antes de escribir para poder repetirse en cada arranque sin
    /// duplicar puntos.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Table<T> con los MISMOS modelos LINQ que usa el repositorio en
            // producción: mismo mapeo, mismas particiones, mismo TTL de tabla.
            var historial = new Table<HistorialPrecioPorProducto>(session);
            var vistas = new Table<VistaPorUsuario>(session);

            await SeedHistorialAsync(historial);
            await SeedVistasAsync(vistas);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Error al sembrar datos en Cassandra (puede que el nodo no este disponible)");
        }
    }

    /// <summary>
    /// Serie de precios de los 3 productos seed. Cada serie termina en el precio
    /// vigente del catálogo (el punto más reciente enlaza con el precio actual).
    /// </summary>
    private async Task SeedHistorialAsync(Table<HistorialPrecioPorProducto> historial)
    {
        var yaSembrado = (await historial
            .Where(h => h.ProductoId == 1)
            .Take(1)
            .ExecuteAsync()).Any();

        if (yaSembrado)
        {
            logger.LogInformation("Cassandra ya contiene historial, omitiendo sembrado");
            return;
        }

        logger.LogInformation("Sembrando historial de precios en Cassandra...");

        // (producto, [(días hacia atrás, precio), ...]) — el último de cada uno es
        // el precio actual de SqlSeeder: 1299.99 / 29.99 / 42.99.
        var series = new (long ProductoId, (int Dias, double Precio)[] Puntos)[]
        {
            (1, [(90, 1199.99), (75, 1249.99), (60, 1279.99), (40, 1349.99), (25, 1319.99), (10, 1299.99)]),
            (2, [(80, 34.99), (55, 32.99), (30, 31.99), (12, 29.99)]),
            (3, [(95, 46.99), (60, 44.99), (20, 42.99)])
        };

        var totalPuntos = 0;

        foreach (var (productoId, puntos) in series)
        {
            foreach (var (dias, precio) in puntos)
            {
                var fecha = DateTime.UtcNow.AddDays(-dias);

                // Dual-write EXACTO de RegistrarPrecioAsync (Q-A + Q-C) pero con la
                // fecha backdateada: la firma del repo pone UtcNow a propósito (son
                // los cambios "hoy" de los updates reales), así que el seeder repite
                // los dos inserts a mano con su propia fecha.
                await historial.Insert(new HistorialPrecioPorProducto
                {
                    ProductoId = productoId,
                    Fecha = fecha,
                    Precio = precio
                }).ExecuteAsync();

                await session.ExecuteAsync(new SimpleStatement(
                    "INSERT INTO cambios_precio_por_fecha (fecha, producto_id, precio) VALUES (?, ?, ?)",
                    new LocalDate(fecha.Year, fecha.Month, fecha.Day),
                    productoId,
                    precio));

                totalPuntos++;
            }
        }

        logger.LogInformation(
            "Historial de precios sembrado en Cassandra (3 productos, {Puntos} puntos backdateados)",
            totalPuntos);
    }

    /// <summary>
    /// Vistas de ejemplo del usuario seed userdaw (id 2, el mismo que usa
    /// MongoDbSeeder) para que el feed arranque con contenido ordenado.
    /// </summary>
    private async Task SeedVistasAsync(Table<VistaPorUsuario> vistas)
    {
        var yaSembrado = (await vistas
            .Where(v => v.UsuarioId == 2)
            .Take(1)
            .ExecuteAsync()).Any();

        if (yaSembrado)
        {
            logger.LogInformation("Cassandra ya contiene vistas, omitiendo sembrado");
            return;
        }

        logger.LogInformation("Sembrando feed de ejemplo en Cassandra...");

        // Nombres COPIADOS en la fila (denormalización, igual que T11) con los
        // productos seed de SqlSeeder. Horas decrecientes = orden del feed.
        var vistasSeed = new (int HorasAtras, long ProductoId, string Nombre)[]
        {
            (1, 1, "Laptop Dell XPS 15"),
            (4, 3, "Clean Code"),
            (7, 2, "Camiseta Nike"),
            (26, 1, "Laptop Dell XPS 15"),
            (50, 3, "Clean Code")
        };

        foreach (var (horas, productoId, nombre) in vistasSeed)
        {
            await vistas.Insert(new VistaPorUsuario
            {
                UsuarioId = 2,
                Fecha = DateTime.UtcNow.AddHours(-horas),
                ProductoId = productoId,
                Nombre = nombre
            }).ExecuteAsync();
        }

        logger.LogInformation("Feed de ejemplo sembrado en Cassandra (5 vistas de userdaw)");
    }
}
