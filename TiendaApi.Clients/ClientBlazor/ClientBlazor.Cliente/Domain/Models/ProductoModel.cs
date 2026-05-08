namespace ClientBlazor.Cliente.Domain.Models;

/// <summary>
/// Modelo de dominio para representar un producto en la interfaz de usuario.
/// Incluye propiedades calculadas y formateo específico para la vista.
/// </summary>
public record ProductoModel
{
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string? Imagen { get; init; }
    public long CategoriaId { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Obtiene la URL absoluta de la imagen o una ruta a un marcador de posición si no existe.
    /// </summary>
    public string ImagenUrl => string.IsNullOrEmpty(Imagen) 
        ? "/images/placeholder.png" 
        : (Imagen.StartsWith("http") ? Imagen : $"http://localhost:5031/storage/{Imagen}");

    /// <summary>
    /// Devuelve el precio del producto formateado como moneda local.
    /// </summary>
    public string PrecioFormateado => Precio.ToString("C2");

    /// <summary>
    /// Indica si el producto se ha quedado sin existencias.
    /// </summary>
    public bool SinStock => Stock <= 0;

    /// <summary>
    /// Indica si el stock es crítico (mayor que 0 pero menor o igual a 5).
    /// </summary>
    public bool StockBajo => Stock > 0 && Stock <= 5;
}
