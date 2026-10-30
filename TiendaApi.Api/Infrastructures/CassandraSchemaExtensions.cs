using Cassandra;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ISession = Cassandra.ISession;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Creación del keyspace en Cassandra. En Cassandra no hay migraciones de EF:
/// el esquema se declara con CREATE ... IF NOT EXISTS y se puede ampliar en caliente,
/// sin plan de migraciones ni scripts de actualización.
/// </summary>
public static class CassandraSchemaExtensions
{
    /// <summary>
    /// Espera a que el nodo acepte conexiones y crea el keyspace si no existe.
    ///
    /// La imagen oficial de Cassandra tarda ~30-60 s en estar lista (es una JVM):
    /// por eso el primer statement se reintenta hasta 30 veces con 2 s de espera
    /// en vez de rendirse al primer intento.
    /// </summary>
    /// <param name="session">Sesión de Cassandra.</param>
    /// <param name="keyspace">Keyspace de la aplicación.</param>
    /// <returns>Tarea asíncrona.</returns>
    public static async Task InitializeSchemaAsync(this ISession session, string keyspace)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                await session.ExecuteAsync(new SimpleStatement("SELECT now() FROM system.local"));
                break;
            }
            catch (NoHostAvailableException) when (intento < 30)
            {
                Log.Information("Cassandra aún no acepta conexiones (intento {Intento}/30)...", intento);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        // SimpleStrategy y factor 1: suficiente para un nodo único de desarrollo.
        // En producción iría NetworkTopologyStrategy con réplicas por datacenter.
        await session.ExecuteAsync(new SimpleStatement(
            $"CREATE KEYSPACE IF NOT EXISTS {keyspace} " +
            "WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1}"));

        session.ChangeKeyspace(keyspace);

        // ── Q-A: histórico de precios de un producto ─────────────────────────
        // Partition key = producto_id (leer el histórico es leer una partición);
        // clustering key = fecha con orden DESC GUARDADO en la tabla: los puntos
        // salen del más reciente al más antiguo sin ningún ORDER BY.
        // Sin bucket por producto: un producto cambia de precio unas pocas veces
        // al año, la partición no crece (decisión de diseño documentada en doc/09).
        await session.ExecuteAsync(new SimpleStatement("""
            CREATE TABLE IF NOT EXISTS historial_precios_por_producto (
                producto_id bigint,
                fecha timestamp,
                precio double,
                PRIMARY KEY (producto_id, fecha)
            ) WITH CLUSTERING ORDER BY (fecha DESC)
            """));

        // ── Q-C: qué precios cambiaron en un día (alimenta informes) ────────
        // Bucketing de serie temporal (T5.2.8): la partición es EL DÍA (date),
        // no el timestamp exacto — si fuera timestamp cada instante sería una
        // partición diminuta e infinita. Acota el crecimiento por diseño.
        await session.ExecuteAsync(new SimpleStatement("""
            CREATE TABLE IF NOT EXISTS cambios_precio_por_fecha (
                fecha date,
                producto_id bigint,
                precio double,
                PRIMARY KEY (fecha, producto_id)
            )
            """));

        // ── Q-B: feed de actividad, últimos productos vistos por usuario ────
        // Mismo patrón Discord del doc: partición por usuario, orden temporal
        // gratis. El TTL nativo (180 días) caduca las filas SOLO, sin jobs de
        // purga, y acota el crecimiento de la partición (decisión 0.3 del plan).
        await session.ExecuteAsync(new SimpleStatement("""
            CREATE TABLE IF NOT EXISTS vistas_por_usuario (
                usuario_id bigint,
                fecha timestamp,
                producto_id bigint,
                nombre text,
                PRIMARY KEY (usuario_id, fecha)
            ) WITH CLUSTERING ORDER BY (fecha DESC)
                AND default_time_to_live = 15552000
            """));

        Log.Information("Esquema de Cassandra listo en el keyspace {Keyspace}", keyspace);
    }

    /// <summary>
    /// Arranque de Cassandra en la aplicación: espera al nodo (hasta ~60 s) y
    /// crea el esquema con CREATE IF NOT EXISTS. Vive en un try propio porque
    /// el resto de la API no depende de Cassandra: solo el histórico de precios
    /// y el feed de actividad.
    /// </summary>
    /// <param name="app">Aplicación en arranque (antes de app.Run()).</param>
    /// <returns>Tarea asíncrona.</returns>
    public static async Task InitializeCassandraAsync(this WebApplication app)
    {
        try
        {
            var settings = app.Services.GetRequiredService<CassandraSettings>();
            await app.Services.GetRequiredService<ISession>().InitializeSchemaAsync(settings.Keyspace);
        }
        catch (Exception ex)
        {
            Log.Warning(ex,
                "Cassandra no disponible tras los reintentos: la API arranca en modo degradado (sin historial de precios ni feed)");
        }
    }
}
