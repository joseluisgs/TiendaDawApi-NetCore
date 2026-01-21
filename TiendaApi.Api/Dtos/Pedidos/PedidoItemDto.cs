namespace TiendaApi.Api.Dtos.Pedidos;

/// <summary>
/// DTO de artículo de línea dentro de un pedido.
/// Representa un producto individual incluido en la orden de compra.
///
/// <remarks>
/// Este DTO es de solo lectura, generado por el servidor al procesar el pedido.
/// Los valores de precio y subtotal se calculan al momento de la creación.
/// </remarks>
/// </summary>
public record PedidoItemDto
(
    /// <summary>
    /// Identificador del producto solicitado.
    /// Referencia al catálogo de productos.
    /// </summary>
    /// <example>101</example>
    long ProductoId,

    /// <summary>
    /// Nombre del producto al momento de realizar el pedido.
    /// Desnormalizado para mantener histórico si el producto se renombra.
    /// </summary>
    /// <example>Laptop HP ProBook</example>
    string NombreProducto,

    /// <summary>
    /// Cantidad solicitada del producto.
    /// Entero positivo mayor a cero.
    /// </summary>
    /// <example>2</example>
    int Cantidad,

    /// <summary>
    /// Precio unitario del producto al momento de la compra.
    /// Puede diferir del precio actual del producto si hubo cambios.
    /// </summary>
    /// <example>499.99</example>
    decimal Precio,

    /// <summary>
    /// Subtotal calculado: Cantidad × Precio unitario.
    /// </summary>
    /// <example>999.98</example>
    decimal Subtotal
);
