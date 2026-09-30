using CSharpFunctionalExtensions;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.GraphQL.Inputs;
using TiendaApi.Api.Repositories.Productos;
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
    private readonly IProductoService _productoService;
    private readonly IProductoRepository _productoRepository;

    /// <summary>Constructor para tests.</summary>
    public ProductoMutation(IProductoService productoService, IProductoRepository productoRepository)
    {
        _productoService = productoService;
        _productoRepository = productoRepository;
    }

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

    /// <summary>Actualiza un producto existente.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="input">Campos a modificar.</param>
    /// <param name="service">Servicio de productos.</param>
    /// <returns>Producto actualizado.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto> UpdateProducto(
        long id,
        UpdateProductoInput input,
        [Service] IProductoService service)
    {
        // 🎓 Leer del MODELO DE ESCRITURA (PostgreSQL), no de la caché
        var existing = await _productoRepository.FindByIdAsync(id);
        if (existing is null)
            throw new Exception($"Producto con ID {id} no encontrado");

        var dto = new ProductoRequestDto
        {
            Nombre = input.Nombre ?? existing.Nombre,
            Descripcion = input.Descripcion ?? existing.Descripcion,
            Precio = input.Precio ?? existing.Precio,
            Stock = input.Stock ?? existing.Stock,
            Imagen = input.Imagen ?? existing.Imagen,
            CategoriaId = input.CategoriaId ?? existing.CategoriaId
        };
        var result = await service.UpdateAsync(id, dto);
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
