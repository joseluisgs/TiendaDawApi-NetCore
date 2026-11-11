using HotChocolate.Types;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Api.GraphQL.Types;

/// <summary>
/// Tipo de GraphQL para <see cref="ProductoDto"/>: el MISMO contrato de
/// salida que sirve REST.
///
/// 🎓 Opción "un solo contrato": los types se construyen sobre los DTOs (no
/// sobre las entidades), con <c>BindFieldsExplicitly</c>, para que el esquema
/// documente exactamente lo que se sirve de verdad. Los campos internos de la
/// entidad (isDeleted, rowVersion, categoriaId, proveedorId, helpers de
/// dominio como isLocalImage) NO se exponen: si algún día hacen falta, se
/// añaden al DTO (aditivo, REST no se rompe) y los ganan los dos canales.
/// </summary>
public class ProductoType : ObjectType<ProductoDto> {
    /// <summary>
    /// Configura los campos y descripciones del tipo GraphQL de ProductoDto.
    /// </summary>
    /// <param name="descriptor">Descriptor del tipo de objeto.</param>
    protected override void Configure(IObjectTypeDescriptor<ProductoDto> descriptor) {
        descriptor.Name("ProductoDto");
        descriptor.Description("Producto tal como lo sirve la API (mismo contrato que REST).");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id).Description("Identificador del producto");
        descriptor.Field(p => p.Nombre).Description("El nombre del producto");
        descriptor.Field(p => p.Descripcion).Description("La descripción del producto");
        descriptor.Field(p => p.Precio).Description("El precio del producto");
        descriptor.Field(p => p.Stock).Description("Cantidad en stock");
        descriptor.Field(p => p.Imagen).Description("URL de la imagen (null si no tiene)");
        descriptor.Field(p => p.Categoria).Description("La categoría del producto (resumen: id y nombre)");
        descriptor.Field(p => p.Proveedor).Description("El proveedor del producto (resumen: código y nombre; null si no tiene)");
        descriptor.Field(p => p.CreatedAt).Description("Fecha de creación");
        descriptor.Field(p => p.UpdatedAt).Description("Fecha de última actualización");
    }
}
