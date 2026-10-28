using CSharpFunctionalExtensions;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.GraphQL.Inputs;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.GraphQL.Mutations;

/// <summary>
/// Mutations de GraphQL para productos (requiere rol ADMIN).
///
/// 🎓 GraphQL: en vez de devolver null silencioso, lanzamos excepción
/// para que el cliente vea el error en el array "errors" de la respuesta.
///
/// 🎓 Read-before-write: para hacer merge de campos null, leemos del
/// modelo de ESCRITURA (PostgreSQL), nunca de la caché. Así evitamos
/// escribir encima de un dato reciente que aún no se reflejó en caché.
/// </summary>
public class ProductoMutation
{
    /// <summary>
    /// 🎓 Las dependencias scoped (repositorio/servicio) se inyectan por PARÁMETRO
    /// [Service], nunca por constructor: HotChocolate resuelve el tipo de mutación
    /// una sola vez (lifetime de singleton) y una dependencia scoped capturada en el
    /// constructor conservaría un DbContext YA DISPUESTO tras la primera petición
    /// ("Cannot access a disposed context instance").
    /// </summary>

    /// <summary>Crea un nuevo producto.</summary>
    /// <param name="input">Datos del producto.</param>
    /// <param name="service">Servicio de productos.</param>
    /// <returns>Producto creado.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto> CreateProducto(
        CreateProductoInput input,
        [Service] IProductoService service)
    {
        var dto = new ProductoRequestDto
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion ?? string.Empty,
            Precio = input.Precio,
            Stock = input.Stock,
            Imagen = input.Imagen,
            CategoriaId = input.CategoriaId
        };
        var result = await service.CreateAsync(dto);
        if (result.IsFailure)
            throw new Exception(result.Error.Message);

        return result.Value;
    }

    /// <summary>
    /// Actualiza un producto existente (merge de campos: solo se envían los que cambian).
    ///
    /// 🎓 El mutation es DELGADO: solo mapea el input al DTO de parche y delega en
    /// el servicio. La lectura previa (read-before-write) y el merge de campos
    /// null viven DENTRO de UpdatePartialAsync, como en el endpoint REST PATCH.
    /// Un mutation no debe inyectar repositorios: la lógica de negocio vive en
    /// los servicios.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="input">Campos a modificar (null = no tocar).</param>
    /// <param name="service">Servicio de productos.</param>
    /// <returns>Producto actualizado.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto> UpdateProducto(
        long id,
        UpdateProductoInput input,
        [Service] IProductoService service)
    {
        var patch = new ProductoPatchDto
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            Precio = input.Precio,
            Stock = input.Stock,
            Imagen = input.Imagen,
            CategoriaId = input.CategoriaId
        };

        var result = await service.UpdatePartialAsync(id, patch);
        if (result.IsFailure)
            throw new Exception(result.Error.Message);

        return result.Value;
    }

    /// <summary>Elimina un producto (soft delete).</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="service">Servicio de productos.</param>
    /// <returns>Éxito.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<bool> DeleteProducto(
        long id,
        [Service] IProductoService service)
    {
        var result = await service.DeleteAsync(id);
        if (result.IsFailure)
            throw new Exception(result.Error.Message);

        return true;
    }
}
