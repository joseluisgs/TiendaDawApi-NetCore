using System.ComponentModel;
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
/// Mutations de GraphQL para operaciones CRUD sobre productos.
/// </summary>
/// <remarks>
/// Todas las operaciones requieren rol <c>ADMIN</c>.
/// Las validaciones y reglas de negocio son idénticas a la API REST.
/// <para><b>Códigos de error:</b></para>
/// <list type="bullet">
///   <item><c>NOT_FOUND</c>: El producto no existe</item>
///   <item><c>VALIDATION</c>: Datos inválidos (precio ≤ 0, nombre vacío, etc.)</item>
///   <item><c>CONFLICT</c>: Ya existe producto con ese nombre</item>
///   <item><c>BUSINESS_RULE_VIOLATION</c>: Categoría no existe o tiene pedidos asociados</item>
/// </list>
/// </remarks>
public class ProductoMutation
{
    /// <summary>
    /// Crea un nuevo producto en el sistema.
    /// </summary>
    /// <param name="input">Datos del producto a crear</param>
    /// <param name="service">Servicio de productos (inyectado)</param>
    /// <returns>El producto creado en caso de éxito, o un error</returns>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item>Nombre obligatorio (3-200 caracteres)</item>
    ///   <item>Precio mayor a 0</item>
    ///   <item>Stock no negativo</item>
    ///   <item>Categoría existente</item>
    ///   <item>Imagen URL válida (opcional)</item>
    /// </list>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><c>Result.Success</c>: Producto creado con ID asignado</item>
    ///   <item><c>Result.Failure (VALIDATION)</c>: Datos inválidos</item>
    ///   <item><c>Result.Failure (NOT_FOUND)</c>: Categoría no existe</item>
    ///   <item><c>Result.Failure (CONFLICT)</c>: Nombre duplicado</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// mutation {
    ///   createProducto(input: {nombre: "Laptop", precio: 999.99, stock: 10, categoriaId: 1}) {
    ///     id
    ///     nombre
    ///     precio
    ///   }
    /// }
    /// </code>
    /// </example>
    [Authorize(policy: "AdminOnly")]
    [Description("Crea un nuevo producto (requiere rol ADMIN)")]
    public async Task<Result<ProductoDto, DomainError>> CreateProducto(
        [Description("Datos del nuevo producto")]
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

    /// <summary>
    /// Actualiza un producto existente.
    /// </summary>
    /// <param name="id">ID del producto a actualizar</param>
    /// <param name="input">Campos a modificar (todos opcionales)</param>
    /// <param name="service">Servicio de productos (inyectado)</param>
    /// <returns>El producto actualizado o un error</returns>
    /// <remarks>
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item>Solo se modifican los campos proporcionados (no null)</item>
    ///   <item>Si se modifica el nombre, debe ser único</item>
    ///   <item>Si se modifica la categoría, debe existir</item>
    /// </list>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><c>Result.Success</c>: Producto actualizado</item>
    ///   <item><c>Result.Failure (NOT_FOUND)</c>: El producto no existe</item>
    ///   <item><c>Result.Failure (CONFLICT)</c>: El nuevo nombre ya está en uso</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// mutation {
    ///   updateProducto(id: 1, input: {precio: 899.99, stock: 15}) {
    ///     id
    ///     nombre
    ///     precio
    ///     stock
    ///   }
    /// }
    /// </code>
    /// </example>
    [Authorize(policy: "AdminOnly")]
    [Description("Actualiza un producto existente (requiere rol ADMIN)")]
    public async Task<Result<ProductoDto, DomainError>> UpdateProducto(
        [Description("ID del producto a actualizar")]
        long id,
        [Description("Campos a modificar (opcionales)")]
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

    /// <summary>
    /// Elimina un producto (soft delete).
    /// </summary>
    /// <param name="id">ID del producto a eliminar</param>
    /// <param name="service">Servicio de productos (inyectado)</param>
    /// <returns>Éxito o error con código específico</returns>
    /// <remarks>
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item>Eliminación lógica (soft delete): IsDeleted = true</item>
    ///   <item>El producto no aparece en consultas pero permanece en BD</item>
    /// </list>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><c>UnitResult.Success</c>: Producto eliminado correctamente</item>
    ///   <item><c>UnitResult.Failure (NOT_FOUND)</c>: El producto no existe</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// mutation {
    ///   deleteProducto(id: 1)
    /// }
    /// </code>
    /// </example>
    [Authorize(policy: "AdminOnly")]
    [Description("Elimina un producto (soft delete, requiere rol ADMIN)")]
    public async Task<UnitResult<DomainError>> DeleteProducto(
        [Description("ID del producto a eliminar")]
        long id,
        [Service] IProductoService service)
    {
        return await service.DeleteAsync(id);
    }
}
