namespace TiendaApi.Api.Errors.Proveedores;

/// <summary>
/// Errores del dominio de proveedores (HTTP 404, 409, 400, 422).
/// </summary>
public static class ProveedorError {
    /// <summary>Crea error para proveedor no encontrado.</summary>
    /// <param name="codigo">Código de 11 caracteres del proveedor.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError NotFound(string codigo) =>
        new($"Recurso con código {codigo} no encontrado");

    /// <summary>Crea error para nombre de proveedor duplicado.</summary>
    /// <param name="nombre">Nombre duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError NombreDuplicado(string nombre) =>
        ConflictError.Duplicate("proveedor", nombre);

    /// <summary>Crea error para código de proveedor duplicado.</summary>
    /// <param name="codigo">Código duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError CodigoDuplicado(string codigo) =>
        ConflictError.Duplicate("proveedor", codigo);

    /// <summary>Crea error si el proveedor tiene productos asociados.</summary>
    /// <param name="codigo">Código del proveedor.</param>
    /// <param name="productosCount">Número de productos asociados.</param>
    /// <returns>BusinessRuleError (HTTP 422).</returns>
    public static BusinessRuleError TieneProductos(string codigo, int productosCount) =>
        new($"No se puede eliminar el proveedor con código {codigo} porque tiene {productosCount} productos asociados");

    /// <summary>Crea error de validación simple.</summary>
    /// <param name="mensaje">Mensaje de error.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError Validacion(string mensaje) =>
        ValidationError.Create(mensaje);

    /// <summary>Crea error de validación con detalles por campo.</summary>
    /// <param name="errores">Diccionario de errores por campo.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}
