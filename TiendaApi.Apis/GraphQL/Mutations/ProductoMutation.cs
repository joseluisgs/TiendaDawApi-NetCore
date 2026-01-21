using CSharpFunctionalExtensions;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.GraphQL.Inputs;
using TiendaApi.Apis.Services.Productos;

namespace TiendaApi.Apis.GraphQL.Mutations;

/// <summary>
/// Mutations de GraphQL para productos (requiere rol ADMIN).
/// </summary>
public class ProductoMutation
{
    private readonly IProductoService _productoService;

    /// <summary>Constructor para tests.</summary>
    public ProductoMutation(IProductoService productoService) => _productoService = productoService;

    /// <summary>Crea un nuevo producto.</summary>
    /// <param name="input">Datos del producto.</param>
    /// <param name="service">Servicio de productos.</param>
    /// <returns>Producto creado o error.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<Result<ProductoDto, DomainError>> CreateProducto(
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
        return await service.CreateAsync(dto);
    }

    /// <summary>Actualiza un producto existente.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="input">Campos a modificar.</param>
    /// <param name="service">Servicio de productos.</param>
    /// <returns>Producto actualizado o error.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<Result<ProductoDto, DomainError>> UpdateProducto(
        long id,
        UpdateProductoInput input,
        [Service] IProductoService service)
    {
        var existingResult = await service.FindByIdAsync(id);
        if (existingResult.IsFailure)
            return existingResult;

        var dto = new ProductoRequestDto
        {
            Nombre = input.Nombre ?? existingResult.Value.Nombre,
            Descripcion = input.Descripcion ?? existingResult.Value.Descripcion,
            Precio = input.Precio ?? existingResult.Value.Precio,
            Stock = input.Stock ?? existingResult.Value.Stock,
            Imagen = input.Imagen ?? existingResult.Value.Imagen,
            CategoriaId = input.CategoriaId ?? existingResult.Value.CategoriaId
        };
        return await service.UpdateAsync(id, dto);
    }

    /// <summary>Elimina un producto (soft delete).</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="service">Servicio de productos.</param>
    /// <returns>Éxito o error.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<UnitResult<DomainError>> DeleteProducto(
        long id,
        [Service] IProductoService service) => await service.DeleteAsync(id);
}
