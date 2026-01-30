using TiendaApi.Api.Dtos.Common;

namespace TiendaApi.Api.Dtos.Pedidos;

/// <summary>
/// Objeto de transferencia para la visualización detallada de un pedido.
/// </summary>
public record PedidoDto
{
    /// <summary>Identificador único (GUID) del pedido.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Identificador del usuario que realizó la compra.</summary>
    public long UserId { get; init; }

    /// <summary>Información sobre el destinatario y la dirección de envío.</summary>
    public DestinatarioDto? Destinatario { get; init; }

    /// <summary>Lista de productos incluidos en el pedido.</summary>
    public List<PedidoItemDto> Items { get; init; } = new();

    /// <summary>Importe total del pedido.</summary>
    public decimal Total { get; init; }

    /// <summary>Estado actual (PENDIENTE, ENVIADO, etc.).</summary>
    public string Estado { get; init; } = string.Empty;

    /// <summary>Fecha de creación.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Fecha de última modificación.</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Representa un artículo individual dentro de un pedido.
/// </summary>
public record PedidoItemDto
{
    /// <summary>ID del producto.</summary>
    public long ProductoId { get; init; }

    /// <summary>Nombre del producto en el momento de la compra.</summary>
    public string NombreProducto { get; init; } = string.Empty;

    /// <summary>Unidades adquiridas.</summary>
    public int Cantidad { get; init; }

    /// <summary>Precio unitario aplicado.</summary>
    public decimal Precio { get; init; }

    /// <summary>Subtotal por este artículo.</summary>
    public decimal Subtotal { get; init; }
}

/// <summary>
/// DTO para la creación de un nuevo pedido.
/// </summary>
public record PedidoRequestDto
{
    /// <summary>Información del envío.</summary>
    public DestinatarioDto? Destinatario { get; init; }

    /// <summary>Listado de artículos solicitados.</summary>
    public List<PedidoItemRequestDto> Items { get; init; } = new();
}

/// <summary>
/// Solicitud de un artículo individual.
/// </summary>
public record PedidoItemRequestDto
{
    /// <summary>Identificador del producto deseado.</summary>
    public long ProductoId { get; init; }

    /// <summary>Cantidad a comprar.</summary>
    public int Cantidad { get; init; }
}