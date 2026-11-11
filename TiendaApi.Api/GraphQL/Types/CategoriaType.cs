using TiendaApi.Api.Dtos.Categorias;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
///     Tipo de GraphQL para <see cref="CategoriaDto" />: el MISMO contrato de
///     salida que sirve REST.
///     🎓 Opción "un solo contrato": sobre el DTO, con <c>BindFieldsExplicitly</c>.
///     El campo <c>productos</c> de la entidad NO se expone: el listado de
///     productos de una categoría se pide por la query <c>productos</c>
///     (REST: GET /api/v1/productos/categoria/{id}), no por navegación anidada.
/// </summary>
public class CategoriaType : ObjectType<CategoriaDto> {
    /// <summary>
    ///     Configura los campos y descripciones del tipo GraphQL de CategoriaDto.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<CategoriaDto> descriptor) {
        descriptor.Name("CategoriaDto");
        descriptor.Description("Categoría tal como la sirve la API (mismo contrato que REST).");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(c => c.Id).Description("El ID de la categoría");
        descriptor.Field(c => c.Nombre).Description("El nombre de la categoría");
        descriptor.Field(c => c.Descripcion).Description("La descripción de la categoría");
        descriptor.Field(c => c.CreatedAt).Description("Fecha de creación");
        descriptor.Field(c => c.UpdatedAt).Description("Fecha de última actualización");
    }
}
