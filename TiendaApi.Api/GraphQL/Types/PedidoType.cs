using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
///     Tipo de GraphQL para <see cref="PedidoDto" />: el MISMO contrato de salida
///     que sirve REST.
///     🎓 Opción "un solo contrato": sobre el DTO, con <c>BindFieldsExplicitly</c>.
///     Solo QUERIES (no mutations ni subscriptions): los pedidos se crean y
///     modifican por REST; GraphQL los expone para lectura con control de acceso
///     (pedidos = solo admin, misPedidos = solo el usuario autenticado).
/// </summary>
public class PedidoType : ObjectType<PedidoDto> {
    /// <summary>
    ///     Configura los campos y descripciones del tipo GraphQL de PedidoDto.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<PedidoDto> descriptor) {
        descriptor.Name("PedidoDto");
        descriptor.Description("Pedido tal como lo sirve la API (mismo contrato que REST).");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id).Description("Identificador del pedido");
        descriptor.Field(p => p.UserId).Description("ID del usuario que realizó el pedido");
        descriptor.Field(p => p.Destinatario)
            .Description("Destinatario del pedido (nombre, email, teléfono y dirección)");
        descriptor.Field(p => p.Items).Description("Artículos del pedido");
        descriptor.Field(p => p.Total).Description("Total del pedido");
        descriptor.Field(p => p.Estado)
            .Description("Estado actual (Pendiente, Procesando, Enviado, Entregado, Cancelado)");
        descriptor.Field(p => p.DireccionEnvio)
            .Description("Dirección de envío (deprecated: usar destinatario.direccion)");
        descriptor.Field(p => p.CreatedAt).Description("Fecha de creación");
    }
}
