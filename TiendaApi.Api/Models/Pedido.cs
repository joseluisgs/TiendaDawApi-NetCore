using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TiendaApi.Api.Models;

using TiendaApi.Api.Data.Abstractions;

/// <summary>
/// Entidad de dominio que representa un pedido.
/// Documento embebido con items, total y estado.
/// Estados: PENDIENTE → PROCESANDO → ENVIADO → ENTREGADO | CANCELADO.
/// </summary>
public class Pedido : ITimestamped
{
    /// <summary>Identificador ObjectId generado automáticamente por MongoDB.</summary>
    [BsonId]
    [Key]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>Usuario que realizó el pedido (FK a PostgreSQL).</summary>
    public long UserId { get; set; }

    /// <summary>Persona que recibirá el pedido (null = mismo usuario).</summary>
    public Destinatario? Destinatario { get; set; }

    /// <summary>Lista de items del pedido (documentos embebidos).</summary>
    public List<PedidoItem> Items { get; set; } = new();

    /// <summary>Total del pedido (suma de subtotales).</summary>
    public decimal Total { get; set; }

    /// <summary>Estado actual del pedido.</summary>
    [MaxLength(50)]
    public string Estado { get; set; } = PedidoEstado.PENDIENTE;

    /// <summary>Dirección de envío.</summary>
    [MaxLength(500)]
    public string? DireccionEnvio { get; set; }

    /// <summary>Indica si el pedido está eliminado (soft-delete).</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Elemento individual dentro de un pedido.
/// Registra producto, cantidad, precio y subtotal al momento de la compra.
/// </summary>
public class PedidoItem
{
    /// <summary>ID del producto en el catálogo.</summary>
    public long ProductoId { get; set; }

    /// <summary>Nombre del producto al momento de la compra (histórico).</summary>
    [MaxLength(200)]
    public string NombreProducto { get; set; } = string.Empty;

    /// <summary>Cantidad de unidades (debe ser > 0).</summary>
    public int Cantidad { get; set; }

    /// <summary>Precio unitario al momento de la compra (histórico).</summary>
    public decimal Precio { get; set; }

    /// <summary>Subtotal calculado (Cantidad × Precio).</summary>
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Constantes para los estados de un pedido.
/// Flujo típico: PENDIENTE → PROCESANDO → ENVIADO → ENTREGADO
/// Flujo alternativo: PENDIENTE → CANCELADO
/// </summary>
public static class PedidoEstado
{
    /// <summary>Pedido creado pero sin confirmar (estado inicial).</summary>
    public const string PENDIENTE = "PENDIENTE";

    /// <summary>Pedido confirmado y en preparación.</summary>
    public const string PROCESANDO = "PROCESANDO";

    /// <summary>Pedido en camino al cliente.</summary>
    public const string ENVIADO = "ENVIADO";

    /// <summary>Pedido entregado al cliente (estado final).</summary>
    public const string ENTREGADO = "ENTREGADO";

    /// <summary>Pedido cancelado (desde cualquier estado).</summary>
    public const string CANCELADO = "CANCELADO";
}
