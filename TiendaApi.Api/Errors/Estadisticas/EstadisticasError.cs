namespace TiendaApi.Api.Errors.Estadisticas;

/// <summary>
/// Errores del dominio de estadísticas (HTTP 400 y 404).
/// </summary>
public static class ErrorEstadisticas
{
    /// <summary>Crea error cuando la agrupación temporal no está soportada.</summary>
    /// <param name="agrupar">Valor recibido en la query string.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError AgrupacionInvalida(string agrupar) =>
        ValidationError.Create(
            $"La agrupación '{agrupar}' no es válida: usa dia, semana o mes");

    /// <summary>Crea error cuando el producto no tiene histórico de precios.</summary>
    /// <param name="productoId">ID del producto.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError SerieVacia(long productoId) =>
        new($"El producto {productoId} no tiene histórico de precios");
}
