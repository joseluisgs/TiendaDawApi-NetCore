using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace TiendaApi.Apis.Models;

/// <summary>
/// Documento de pedido en MongoDB.
/// </summary>
public class Pedido
{
    /// <summary>
    /// Identificador único del pedido.
    /// </summary>
    [Key]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// Identificador del usuario que realizó el pedido.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Lista de elementos del pedido.
    /// </summary>
    public List<PedidoItem> Items { get; set; } = new();

    /// <summary>
    /// Total del pedido.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Estado del pedido.
    /// </summary>
    [MaxLength(50)]
    public string Estado { get; set; } = PedidoEstado.PENDIENTE;

    /// <summary>
    /// Fecha de creación del pedido.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización del pedido.
    /// </summary>
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
    public long ProductoId { get; set; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    [MaxLength(200)]
    public string NombreProducto { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad del producto en el pedido.
    /// </summary>
    public int Cantidad { get; set; }

    /// <summary>
    /// Precio unitario del producto.
    /// </summary>
    public decimal Precio { get; set; }

    /// <summary>
    /// Subtotal del elemento (cantidad * precio).
    /// </summary>
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
