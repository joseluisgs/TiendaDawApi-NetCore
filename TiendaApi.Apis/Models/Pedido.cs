using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace TiendaApi.Apis.Models;

using TiendaApi.Apis.Data.Abstractions;

/// <summary>
/// Entidad de dominio que representa un pedido en el sistema de la tienda.
/// 
/// <para>
/// Un pedido es el documento que registra una transacción de compra.
/// Contiene la información del cliente, los productos adquiridos, el total
/// y el estado actual del proceso de fulfillment.
/// </para>
/// 
/// <para>
/// <b>Características principales:</b>
/// <list type="bullet">
///   <item><description>Identificador único de MongoDB (ObjectId).</description></item>
///   <item><description>Referencia al usuario que realizó el pedido.</description></item>
///   <item><description>Lista de elementos (productos) con cantidades y precios.</description></item>
///   <item><description>Cálculo automático del total basado en items.</description></item>
///   <item><description>Seguimiento de estado durante el ciclo de vida.</description></item>
///   <item><description>Dirección de envío para fulfillment.</description></item>
///   <item><description>Soft-delete para conservación histórica.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Patrón de documento embebido:</b> Los items del pedido (PedidoItem)
/// se almacenan como documentos embebidos dentro del pedido, no como referencias.
/// Esto garantiza que el historial de precios se preserve incluso si los
/// productos cambian posteriormente.
/// </para>
/// 
/// <para>
/// <b>Estados del pedido:</b> El ciclo de vida típico es:
/// PENDIENTE → PROCESANDO → ENVIADO → ENTREGADO
/// También puede terminar en CANCELADO en cualquier momento.
/// </para>
/// </summary>
/// <example>
/// Crear un pedido:
///
/// <code>
/// var pedido = new Pedido
/// {
///     UserId = 123,
///     Items = new List&lt;PedidoItem&gt;(),
///     DireccionEnvio = "Calle Principal 123, Madrid"
/// };
/// pedido.Items.Add(new PedidoItem { ... });
/// </code>
///
/// Calcular total:
/// <code>
/// pedido.Total = pedido.Items.Sum(i => i.Subtotal);
/// </code>
/// </example>
public class Pedido : ITimestamped
{
    /// <summary>
    /// Identificador único del pedido en MongoDB.
    /// 
    /// <para>
    /// Se genera automáticamente al crear el documento en MongoDB.
    /// Es un ObjectId de 12 bytes que incluye timestamp, máquina,
    /// proceso y contador.
    /// </para>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>Formato: ObjectId de 24 caracteres hexadecimales.</description></item>
    ///   <item><description>Ejemplo: "507f1f77bcf86cd799439011"</description></item>
    ///   <item><description>Ordenable por tiempo de creación.</description></item>
    /// </list>
    /// </remarks>
    [Key]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    /// Identificador del usuario que realizó el pedido.
    ///
    /// <para>
    /// Clave foránea que referencia al usuario en PostgreSQL.
    /// Permite recuperar la información del cliente asociada al pedido.
    /// </para>
    /// <remarks>
    /// Valor ejemplo: 1, 2, 3, ... (long positivo)
    /// </remarks>
    public long UserId { get; set; }

    /// <summary>
    /// Información del destinatario del pedido.
    ///
    /// <para>
    /// Datos de la persona que recibirá el pedido. Puede ser diferente
    /// al usuario que realiza la compra (ej: regalo, envío a trabajo).
    /// </para>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>Si es null, el destinatario es el mismo usuario.</description></item>
    ///   <item><description>Incluye nombre, email, teléfono y dirección estructurada.</description></item>
    /// </list>
    /// </remarks>
    public Destinatario? Destinatario { get; set; }

    /// <summary>
    /// Lista de elementos incluidos en el pedido.
    /// 
    /// <para>
    /// Cada elemento representa un producto con su cantidad, precio unitario
    /// y subtotal. Se almacenan como documentos embebidos para preservar
    /// el historial de precios al momento de la compra.
    /// </para>
    /// <remarks>
    /// La lista nunca debería estar vacía para un pedido válido.
    /// </remarks>
    public List<PedidoItem> Items { get; set; } = new();

    /// <summary>
    /// Total del pedido en la moneda configurada (EUR).
    /// 
    /// <para>
    /// Se calcula como la suma de los subtotales de todos los items.
    /// No incluye costos de envío adicionales.
    /// </para>
    /// <remarks>
    /// Formato: decimal con hasta 2 decimales (ej: 149.99)
    /// </remarks>
    /// <example>
    /// Cálculo automático:
    /// <code>
    /// pedido.Total = pedido.Items.Sum(item => item.Subtotal);
    /// </code>
    /// </example>
    public decimal Total { get; set; }

    /// <summary>
    /// Estado actual del pedido en el ciclo de vida.
    /// 
    /// <para>
    /// <b>Valores posibles:</b>
    /// <list type="bullet">
    ///   <item><term>PENDIENTE</term>: Pedido creado, esperando confirmación.</description></item>
    ///   <item><term>PROCESANDO</term>: Pago confirmado, preparando para envío.</description></item>
    ///   <item><term>ENVIADO</term>: En camino al cliente.</description></item>
    ///   <item><term>ENTREGADO</term>: Recibido por el cliente (finalizado).</description></item>
    ///   <item><term>CANCELADO</term>: Pedido cancelado (reembolsado).</description></item>
    /// </list>
    /// </para>
    /// <remarks>
    /// Valor por defecto: PedidoEstado.PENDIENTE
    /// </remarks>
    [MaxLength(50)]
    public string Estado { get; set; } = PedidoEstado.PENDIENTE;

    /// <summary>
    /// Dirección de envío donde se deliverá el pedido.
    /// 
    /// <para>
    /// Almacena la dirección completa del cliente al momento de la compra.
    /// Puede incluir nombre de calle, número, piso, código postal y ciudad.
    /// </para>
    /// <remarks>
    /// Longitud máxima: 500 caracteres
    /// </remarks>
    [MaxLength(500)]
    public string? DireccionEnvio { get; set; }

    /// <summary>
    /// Indica si el pedido ha sido eliminado (soft-delete).
    /// 
    /// <para>
    /// Los pedidos no se eliminan físicamente para mantener historial
    /// de ventas y cumplimiento legal. Solo se marcan como eliminados
    /// para ocultarlos en listados estándar.
    /// </para>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><term>false</term>: Pedido visible y activo.</item>
    ///   <item><term>true</term>: Pedido eliminado (soft-delete).</item>
    /// </list>
    /// </remarks>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Fecha y hora UTC de creación del pedido.
    /// 
    /// <para>
    /// Se asigna automáticamente al crear el documento.
    /// Es el momento en que el usuario completó la compra.
    /// </para>
    /// <remarks>
    /// Formato: DateTime en UTC (ej: 2024-01-15T10:30:00Z)
    /// </remarks>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora UTC de la última modificación.
    /// 
    /// <para>
    /// Se actualiza cuando cambia el estado del pedido
    /// (por ejemplo, al enviar o entregar).
    /// </para>
    /// <remarks>
    /// Importante para auditoría y cálculo de tiempos de entrega.
    /// </remarks>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Documento embebido que representa un elemento individual dentro de un pedido.
/// 
/// <para>
/// Cada PedidoItem registra un producto específico en el pedido con:
/// <list type="bullet">
///   <item><description>Identificador del producto original.</description></item>
///   <item><description>Nombre del producto al momento de la compra.</description></item>
///   <item><description>Cantidad adquirida.</description></item>
///   <item><description>Precio unitario en ese momento.</description></item>
///   <item><description>Subtotal (cantidad × precio).</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Importante:</b> El nombre y precio se copian del producto al momento
/// de la compra. Si el producto cambia después, el pedido mantiene los valores
/// originales para transparencia en la facturación.
/// </para>
/// </summary>
/// <example>
/// Crear un elemento de pedido:
/// <code>
/// var item = new PedidoItem
/// {
///     ProductoId = 456,
///     NombreProducto = "Laptop HP",
///     Cantidad = 1,
///     Precio = 899.99m,
///     Subtotal = 899.99m
/// };
/// </code>
/// </example>
public class PedidoItem
{
    /// <summary>
    /// Identificador del producto original en el catálogo.
    /// 
    /// <para>
    /// Permite referenciar el producto actual en la base de datos
    /// para obtener información actualizada (si es necesario).
    /// </para>
    /// <remarks>
    /// Valor ejemplo: 1, 2, 3, ... (long positivo)
    /// </remarks>
    public long ProductoId { get; set; }

    /// <summary>
    /// Nombre del producto al momento de la compra.
    /// 
    /// <para>
    /// Se copia del producto en el momento de agregar al pedido.
    /// Este valor se сохраня para mantener historial 即使
    /// el producto se renombre posteriormente.
    /// </para>
    /// <remarks>
    /// Longitud máxima: 200 caracteres
    /// </remarks>
    [MaxLength(200)]
    public string NombreProducto { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad de unidades del producto en el pedido.
    /// 
    /// <para>
    /// Debe ser mayor a 0 para un elemento válido.
    /// Se valida contra el stock disponible antes de confirmar el pedido.
    /// </para>
    /// <remarks>
    /// Valor típico: 1, 2, 3, ... (entero positivo)
    /// </remarks>
    public int Cantidad { get; set; }

    /// <summary>
    /// Precio unitario del producto al momento de la compra.
    /// 
    /// <para>
    /// Se copia del precio actual del producto en el momento
    /// de agregar al pedido. Este valor no cambia aunque
    /// el producto cambie de precio después.
    /// </para>
    /// <remarks>
    /// Formato: decimal con hasta 2 decimales (ej: 19.99)
    /// </remarks>
    public decimal Precio { get; set; }

    /// <summary>
    /// Subtotal del elemento (cantidad × precio unitario).
    /// 
    /// <para>
    /// Se calcula automáticamente como:
    /// Subtotal = Cantidad × Precio
    /// 
    /// Se usa para el cálculo del total del pedido y para
    /// mostrar desglose en la factura.
    /// </para>
    /// <remarks>
    /// Formato: decimal con hasta 2 decimales (ej: 59.97)
    /// </remarks>
    /// <example>
    /// Cálculo:
    /// <code>
    /// item.Subtotal = item.Cantidad * item.Precio;
    /// </code>
    /// </example>
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Constantes para los estados posibles de un pedido.
/// 
/// <para>
/// Define el ciclo de vida de un pedido desde su creación
/// hasta su finalización o cancelación.
/// </para>
/// 
/// <para>
/// <b>Flujo típico:</b>
/// PENDIENTE → PROCESANDO → ENVIADO → ENTREGADO
/// 
/// <b>Flujo alternativo:</b>
/// PENDIENTE → CANCELADO
/// </para>
/// </summary>
public static class PedidoEstado
{
    /// <summary>
    /// Pedido creado pero sin confirmar.
    /// 
    /// <para>
    /// Estado inicial. El pedido está en espera de pago
    /// o validación de stock.
    /// </para>
    /// <remarks>
    /// Duración típica: segundos a minutos
    /// </remarks>
    public const string PENDIENTE = "PENDIENTE";

    /// <summary>
    /// Pedido confirmado y en preparación.
    /// 
    /// <para>
    /// El pago ha sido verificado y el pedido está siendo
    /// preparado para envío (empaquetado, picking).
    /// </para>
    /// <remarks>
    /// Duración típica: horas a 1 día
    /// </remarks>
    public const string PROCESANDO = "PROCESANDO";

    /// <summary>
    /// Pedido en camino al cliente.
    /// 
    /// <para>
    /// Ha sido entregado al servicio de mensajería o logistics.
    /// El cliente puede hacer seguimiento del envío.
    /// </para>
    /// <remarks>
    /// Duración típica: 1-5 días laborables
    /// </remarks>
    public const string ENVIADO = "ENVIADO";

    /// <summary>
    /// Pedido entregado y completado.
    /// 
    /// <para>
    /// El cliente ha recibido el paquete. Estado final
    /// satisfactorio del pedido.
    /// </para>
    /// <remarks>
    /// Estado terminal (no cambia más)
    /// </remarks>
    public const string ENTREGADO = "ENTREGADO";

    /// <summary>
    /// Pedido cancelado (no completado).
    /// 
    /// <para>
    /// El pedido ha sido cancelado antes de la entrega.
    /// Puede requerir reembolso al cliente.
    /// </para>
    /// <remarks>
    /// Estado terminal (no cambia más)
    /// </remarks>
    public const string CANCELADO = "CANCELADO";
}
