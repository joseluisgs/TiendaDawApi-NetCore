using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TiendaApi.Models.Entities;

/// <summary>
/// MongoDB document for orders/pedidos
/// Stored in MongoDB (NoSQL) instead of PostgreSQL
/// 
/// Java equivalent: @Document from Spring Data MongoDB
/// Demonstrates hybrid database architecture
/// </summary>
public class Pedido
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    public long UserId { get; set; }

    [BsonElement("items")]
    public List<PedidoItem> Items { get; set; } = new();

    [BsonElement("total")]
    public decimal Total { get; set; }

    [BsonElement("estado")]
    public string Estado { get; set; } = PedidoEstado.PENDIENTE;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Embedded document for pedido items
/// MongoDB supports embedded documents (unlike SQL joins)
/// </summary>
public class PedidoItem
{
    [BsonElement("productoId")]
    public long ProductoId { get; set; }

    [BsonElement("nombreProducto")]
    public string NombreProducto { get; set; } = string.Empty;

    [BsonElement("cantidad")]
    public int Cantidad { get; set; }

    [BsonElement("precio")]
    public decimal Precio { get; set; }

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Order status constants
/// </summary>
public static class PedidoEstado
{
    public const string PENDIENTE = "PENDIENTE";
    public const string PROCESANDO = "PROCESANDO";
    public const string ENVIADO = "ENVIADO";
    public const string ENTREGADO = "ENTREGADO";
    public const string CANCELADO = "CANCELADO";
}
