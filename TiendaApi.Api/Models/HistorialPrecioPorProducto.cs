using Cassandra.Mapping.Attributes;

namespace TiendaApi.Api.Models;

/// <summary>
/// Serie temporal de precios: una fila por cada cambio (consulta Q-A).
/// El orden temporal está GUARDADO en la tabla con CLUSTERING ORDER BY (fecha DESC),
/// así que "¿a cuánto estuvo el producto hace tres semanas?" sale ordenado de serie.
/// </summary>
[Table("historial_precios_por_producto")]
public class HistorialPrecioPorProducto
{
    /// <summary>Partition key: el histórico de un producto vive entero en su partición.</summary>
    [Column("producto_id")]
    [PartitionKey]
    public long ProductoId { get; set; }

    /// <summary>Clustering key en orden descendente: los cambios recientes salen primero.</summary>
    [Column("fecha")]
    [ClusteringKey]
    public DateTime Fecha { get; set; }

    /// <summary>Precio del producto en ese momento (snapshot; el precio anterior se deriva al leer).</summary>
    [Column("precio")]
    public double Precio { get; set; }
}
