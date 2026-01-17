using System.ComponentModel;
using CSharpFunctionalExtensions;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.GraphQL.Inputs;
using TiendaApi.Apis.Services.Categorias;

namespace TiendaApi.Apis.GraphQL.Mutations;

/// <summary>
/// Mutations de GraphQL para operaciones CRUD sobre categorías.
/// </summary>
/// <remarks>
/// Todas las operaciones requieren rol <c>ADMIN</c>.
/// Las validaciones y reglas de negocio son idénticas a la API REST.
/// <para><b>Códigos de error:</b></para>
/// <list type="bullet">
///   <item><c>NOT_FOUND</c>: La categoría no existe</item>
///   <item><c>VALIDATION</c>: Datos inválidos (nombre vacío)</item>
///   <item><c>CONFLICT</c>: Ya existe categoría con ese nombre</item>
///   <item><c>BUSINESS_RULE_VIOLATION</c>: La categoría tiene productos asociados</item>
/// </list>
/// </remarks>
public class CategoriaMutation
{
    /// <summary>
    /// Crea una nueva categoría en el sistema.
    /// </summary>
    /// <param name="input">Nombre de la categoría a crear</param>
    /// <param name="service">Servicio de categorías (inyectado)</param>
    /// <returns>La categoría creada en caso de éxito, o un error</returns>
    /// <remarks>
    /// <para><b>Validaciones:</b></para>
    /// <list type="bullet">
    ///   <item>Nombre obligatorio y único</item>
    /// </list>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><c>Result.Success</c>: Categoría creada con ID asignado</item>
    ///   <item><c>Result.Failure (VALIDATION)</c>: Nombre vacío o muy largo</item>
    ///   <item><c>Result.Failure (CONFLICT)</c>: Ya existe categoría con ese nombre</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// mutation {
    ///   createCategoria(input: {nombre: "Electrónica"}) {
    ///     id
    ///     nombre
    ///   }
    /// }
    /// </code>
    /// </example>
    [Authorize(policy: "AdminOnly")]
    [Description("Crea una nueva categoría (requiere rol ADMIN)")]
    public async Task<Result<CategoriaDto, DomainError>> CreateCategoria(
        [Description("Nombre de la nueva categoría")]
        CreateCategoriaInput input,
        [Service] ICategoriaService service)
    {
        var dto = new CategoriaRequestDto
        {
            Nombre = input.Nombre
        };

        return await service.CreateAsync(dto);
    }

    /// <summary>
    /// Actualiza una categoría existente.
    /// </summary>
    /// <param name="id">ID de la categoría a actualizar</param>
    /// <param name="input">Nuevo nombre (opcional)</param>
    /// <param name="service">Servicio de categorías (inyectado)</param>
    /// <returns>La categoría actualizada o un error</returns>
    /// <remarks>
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item>Solo se modifica el nombre si se proporciona</item>
    ///   <item>Si se cambia el nombre, debe ser único</item>
    /// </list>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><c>Result.Success</c>: Categoría actualizada</item>
    ///   <item><c>Result.Failure (NOT_FOUND)</c>: La categoría no existe</item>
    ///   <item><c>Result.Failure (CONFLICT)</c>: El nuevo nombre ya está en uso</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// mutation {
    ///   updateCategoria(id: 1, input: {nombre: "Nueva Nombre"}) {
    ///     id
    ///     nombre
    ///   }
    /// }
    /// </code>
    /// </example>
    [Authorize(policy: "AdminOnly")]
    [Description("Actualiza una categoría existente (requiere rol ADMIN)")]
    public async Task<Result<CategoriaDto, DomainError>> UpdateCategoria(
        [Description("ID de la categoría a actualizar")]
        long id,
        [Description("Nuevo nombre (opcional)")]
        UpdateCategoriaInput input,
        [Service] ICategoriaService service)
    {
        var existingResult = await service.FindByIdAsync(id);
        if (existingResult.IsFailure)
            return existingResult;

        var nombre = input.Nombre ?? existingResult.Value.Nombre;

        var dto = new CategoriaRequestDto
        {
            Nombre = nombre
        };

        return await service.UpdateAsync(id, dto);
    }

    /// <summary>
    /// Elimina una categoría (soft delete).
    /// </summary>
    /// <param name="id">ID de la categoría a eliminar</param>
    /// <param name="service">Servicio de categorías (inyectado)</param>
    /// <returns>Éxito o error con código específico</returns>
    /// <remarks>
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item>Eliminación lógica (soft delete): IsDeleted = true</item>
    ///   <item>La categoría no aparece en consultas pero permanece en BD</item>
    /// </list>
    /// <para><b>Restricciones:</b></para>
    /// <list type="bullet">
    ///   <item>No se puede eliminar si tiene productos asociados</item>
    /// </list>
    /// <para><b>Respuestas:</b></para>
    /// <list type="bullet">
    ///   <item><c>UnitResult.Success</c>: Categoría eliminada correctamente</item>
    ///   <item><c>UnitResult.Failure (NOT_FOUND)</c>: La categoría no existe</item>
    ///   <item><c>UnitResult.Failure (BUSINESS_RULE_VIOLATION)</c>: Tiene productos asociados</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// mutation {
    ///   deleteCategoria(id: 1)
    /// }
    /// </code>
    /// </example>
    [Authorize(policy: "AdminOnly")]
    [Description("Elimina una categoría (soft delete, requiere rol ADMIN)")]
    public async Task<UnitResult<DomainError>> DeleteCategoria(
        [Description("ID de la categoría a eliminar")]
        long id,
        [Service] ICategoriaService service)
    {
        return await service.DeleteAsync(id);
    }
}
