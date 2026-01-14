namespace TiendaApi.Apis.Data;

/// <summary>
/// Interfaz para entidades con timestamps automáticos.
/// EF Core automáticamente asigna CreatedAt y UpdatedAt.
/// </summary>
public interface ITimestamped
{
    /// <summary>
    /// Fecha de creación del registro.
    /// Se asigna automáticamente al crear.
    /// </summary>
    DateTime CreatedAt { get; init; }

    /// <summary>
    /// Fecha de última actualización del registro.
    /// Se asigna automáticamente al modificar.
    /// </summary>
    DateTime UpdatedAt { get; init; }
}
