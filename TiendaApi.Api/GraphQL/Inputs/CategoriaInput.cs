namespace TiendaApi.Api.GraphQL.Inputs;

/// <summary>
/// Datos de entrada para crear una categoría.
/// </summary>
public record CreateCategoriaInput
{
    /// <summary>Nombre de la categoría (obligatorio, único).</summary>
    public string Nombre { get; init; } = string.Empty;
}

/// <summary>
/// Datos de entrada para actualizar una categoría.
/// </summary>
public record UpdateCategoriaInput
{
    /// <summary>Nuevo nombre (opcional, null = no modificar).</summary>
    public string? Nombre { get; init; }
}
