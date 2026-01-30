namespace ClientBlazor.Cliente.Domain.Models;

/// <summary>
/// Modelo de dominio para representar una categoría de productos en la interfaz.
/// </summary>
public record CategoriaModel
{
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Devuelve el nombre de la categoría formateado con su identificador.
    /// </summary>
    public string DisplayName => $"{Nombre} (ID: {Id})";
}
