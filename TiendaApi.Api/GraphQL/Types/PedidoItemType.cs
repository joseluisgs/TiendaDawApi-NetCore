using HotChocolate.Types;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para <see cref="PedidoItemDto"/>: línea de artículo del
/// pedido (mismo contrato que REST).
/// </summary>
public class PedidoItemType : ObjectType<PedidoItemDto>
{
    /// <summary>
    /// Configura los campos y descripciones del tipo GraphQL de PedidoItemDto.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<PedidoItemDto> descriptor)
    {
        descriptor.Name("PedidoItemDto");
        descriptor.Description("Artículo del pedido (mismo contrato que REST).");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(i => i.ProductoId).Description("ID del producto comprado");
        descriptor.Field(i => i.NombreProducto).Description("Nombre del producto al comprar (desnormalizado)");
        descriptor.Field(i => i.Cantidad).Description("Cantidad comprada");
        descriptor.Field(i => i.Precio).Description("Precio unitario en el momento de la compra");
        descriptor.Field(i => i.Subtotal).Description("Cantidad × precio unitario");
    }
}
