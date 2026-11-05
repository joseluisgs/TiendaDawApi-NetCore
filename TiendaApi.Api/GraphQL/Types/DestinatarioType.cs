using HotChocolate.Types;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para <see cref="DestinatarioDto"/>: quién recibe el
/// pedido (mismo contrato que REST).
/// </summary>
public class DestinatarioType : ObjectType<DestinatarioDto>
{
    /// <summary>
    /// Configura los campos y descripciones del tipo GraphQL de DestinatarioDto.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<DestinatarioDto> descriptor)
    {
        descriptor.Name("DestinatarioDto");
        descriptor.Description("Destinatario del pedido (mismo contrato que REST).");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(d => d.NombreCompleto).Description("Nombre completo del destinatario");
        descriptor.Field(d => d.Email).Description("Email de contacto del destinatario");
        descriptor.Field(d => d.Telefono).Description("Teléfono del destinatario (opcional)");
        descriptor.Field(d => d.Direccion).Description("Dirección de entrega");
    }
}
