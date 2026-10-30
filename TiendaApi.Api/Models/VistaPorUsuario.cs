using Cassandra.Mapping.Attributes;

namespace TiendaApi.Api.Models;

/// <summary>
/// Fila del feed de actividad: un producto visto por un usuario (consulta Q-B).
/// Mismo patrón Discord del doc: partición por usuario, orden temporal gratis.
/// La tabla declara default_time_to_live = 180 días: el TTL caduca las filas SOLO,
/// sin trabajos de limpieza programados, y acota el crecimiento de la partición.
/// </summary>
[Table("vistas_por_usuario")]
public class VistaPorUsuario
{
    /// <summary>Partition key: toda la actividad de un usuario vive en su partición.</summary>
    [Column("usuario_id")]
    [PartitionKey]
    public long UsuarioId { get; set; }

    /// <summary>Clustering key descendente: las vistas recientes salen primero.</summary>
    [Column("fecha")]
    [ClusteringKey]
    public DateTime Fecha { get; set; }

    /// <summary>Producto visto.</summary>
    [Column("producto_id")]
    public long ProductoId { get; set; }

    /// <summary>Nombre del producto COPIADO en la fila: denormalización (sin joins).</summary>
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;
}
