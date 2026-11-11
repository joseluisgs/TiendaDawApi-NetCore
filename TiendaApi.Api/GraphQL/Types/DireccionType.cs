using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
///     Tipo de GraphQL para <see cref="DireccionDto" />: dirección de entrega
///     (mismo contrato que REST).
/// </summary>
public class DireccionType : ObjectType<DireccionDto> {
    /// <summary>
    ///     Configura los campos y descripciones del tipo GraphQL de DireccionDto.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<DireccionDto> descriptor) {
        descriptor.Name("DireccionDto");
        descriptor.Description("Dirección postal de envío (mismo contrato que REST).");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(d => d.Calle).Description("Calle, avenida, plaza o vía pública");
        descriptor.Field(d => d.Numero).Description("Número del edificio (opcional)");
        descriptor.Field(d => d.Ciudad).Description("Ciudad, pueblo o municipio");
        descriptor.Field(d => d.Provincia).Description("Provincia o región (opcional)");
        descriptor.Field(d => d.Pais).Description("País");
        descriptor.Field(d => d.CodigoPostal).Description("Código postal (5 dígitos, opcional)");
    }
}
