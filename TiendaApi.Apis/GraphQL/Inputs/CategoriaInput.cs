namespace TiendaApi.Apis.GraphQL.Inputs;

/// <summary>
/// Datos de entrada para crear una nueva categoría.
/// </summary>
/// <remarks>
/// <para><b>Validaciones:</b></para>
/// <list type="bullet">
///   <item><description>Nombre: obligatorio, entre 3 y 100 caracteres</description></item>
///   <item><description>Nombre: debe ser único en el sistema</description></item>
/// </list>
/// <para><b>Errores posibles:</b></para>
/// <list type="bullet">
///   <item><description>VALIDATION: nombre vacío o muy largo</description></item>
///   <item><description>CONFLICT: ya existe categoría con ese nombre</description></item>
/// </list>
/// </remarks>
public record CreateCategoriaInput
{
    /// <summary>
    /// Nombre de la categoría. Obligatorio y único.
    /// </summary>
    /// <example>Electrónica</example>
    public string Nombre { get; init; } = string.Empty;
}

/// <summary>
/// Datos de entrada para actualizar una categoría existente.
/// El campo Nombre es opcional: valor null indica "no modificar".
/// </summary>
/// <remarks>
/// <para><b>Comportamiento:</b></para>
/// <list type="bullet">
///   <item><description>Si Nombre es null, no se modifica</description></item>
///   <item><description>Si se modifica el nombre, debe ser único</description></item>
/// </list>
/// <para><b>Errores posibles:</b></para>
/// <list type="bullet">
///   <item><description>NOT_FOUND: la categoría no existe</description></item>
///   <item><description>CONFLICT: el nuevo nombre ya está en uso</description></item>
/// </list>
/// </remarks>
public record UpdateCategoriaInput
{
    /// <summary>
    /// Nuevo nombre de la categoría (opcional).
    /// Si es null, no se modifica el nombre actual.
    /// </summary>
    /// <example>Electrónica y Computación</example>
    public string? Nombre { get; init; }
}
