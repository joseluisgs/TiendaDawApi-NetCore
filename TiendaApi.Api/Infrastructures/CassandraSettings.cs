namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de conexión a Cassandra (sección "Cassandra" de appsettings).
///
/// 🎓 Cassandra se une a los almacenes de la tienda como series temporales:
/// PostgreSQL (fuente de verdad), MongoDB (read model), Redis (caché) y
/// Cassandra (histórico de precios y actividad con TTL).
/// </summary>
public class CassandraSettings
{
    /// <summary>Nombre de la sección en appsettings.json.</summary>
    public const string SectionName = "Cassandra";

    /// <summary>
    /// Nodos de contacto del cluster (los clientes hablan por el puerto 9042).
    /// El default es un array VACÍO a propósito: el valor real lo manda appsettings.
    /// Con un default poblado (["localhost"]), el ConfigurationBinder de .NET 9+
    /// conserva el default y AÑADE el de appsettings → el array sale duplicado
    /// ("localhost,localhost" en el log y dos contact points redundantes).
    /// </summary>
    public string[] Hosts { get; set; } = [];

    /// <summary>Puerto del protocolo CQL.</summary>
    public int Port { get; set; } = 9042;

    /// <summary>Keyspace de la aplicación (el equivalente a una base de datos).</summary>
    public string Keyspace { get; set; } = "tienda";
}
