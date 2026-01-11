using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TiendaApi.Models;

/// <summary>
/// Documento de pedido en MongoDB.
/// </summary>
public class Pedido
{
    /// <summary>
    /// Identificador único del pedido.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Identificador del usuario que realizó el pedido.
    /// </summary>
    [BsonElement("userId")]
    public long UserId { get; set; }

    /// <summary>
    /// Lista de elementos del pedido.
    /// </summary>
    [BsonElement("items")]
    public List<PedidoItem> Items { get; set; } = new();

    /// <summary>
    /// Total del pedido.
    /// </summary>
    [BsonElement("total")]
    public decimal Total { get; set; }

    /// <summary>
    /// Estado del pedido.
    /// </summary>
    [BsonElement("estado")]
    public string Estado { get; set; } = PedidoEstado.PENDIENTE;

    /// <summary>
    /// Fecha de creación del pedido.
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización del pedido.
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Elemento embebido en el documento de pedido.
/// </summary>
public class PedidoItem
{
    /// <summary>
    /// Identificador del producto.
    /// </summary>
    [BsonElement("productoId")]
    public long ProductoId { get; set; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    [BsonElement("nombreProducto")]
    public string NombreProducto { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad del producto en el pedido.
    /// </summary>
    [BsonElement("cantidad")]
    public int Cantidad { get; set; }

    /// <summary>
    /// Precio unitario del producto.
    /// </summary>
    [BsonElement("precio")]
    public decimal Precio { get; set; }

    /// <summary>
    /// Subtotal del elemento (cantidad * precio).
    /// </summary>
    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Constantes para los estados de pedido.
/// </summary>
public static class PedidoEstado
{
    public const string PENDIENTE = "PENDIENTE";
    public const string PROCESANDO = "PROCESANDO";
    public const string ENVIADO = "ENVIADO";
    public const string ENTREGADO = "ENTREGADO";
    public const string CANCELADO = "CANCELADO";
}
