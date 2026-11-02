namespace TiendaApi.Api.Errors.Backup;

/// <summary>
/// Errores del dominio de backup (HTTP 400, 404 y 409).
/// </summary>
public static class BackupError
{
    /// <summary>Crea error cuando el fichero no se puede interpretar.</summary>
    /// <param name="mensaje">Motivo concreto (zip corrupto, CSV malformado, JSON inválido…).</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError FormatoInvalido(string mensaje) =>
        ValidationError.Create($"El backup no se puede leer: {mensaje}");

    /// <summary>Crea error cuando el fichero supera los límites admisibles.</summary>
    /// <param name="detalle">Límite superado y valor recibido.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError DemasiadoGrande(string detalle) =>
        ValidationError.Create($"El backup supera los límites admitidos: {detalle}");

    /// <summary>Crea error cuando el formato pedido en el export no existe.</summary>
    /// <param name="formato">Formato recibido.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError FormatoDesconocido(string formato) =>
        ValidationError.Create($"Formato '{formato}' no soportado: usa zip, json o csv");

    /// <summary>Crea error cuando se pide una entidad que no existe.</summary>
    /// <param name="entidad">Entidad recibida.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError EntidadDesconocida(string entidad) =>
        ValidationError.Create($"Entidad '{entidad}' no soportada: usa productos, categorias o proveedores");

    /// <summary>Crea error cuando el producto referenciado no existe (solo informativo).</summary>
    /// <param name="codigo">Código o id buscado.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError NoEncontrado(string codigo) =>
        new($"Recurso con código {codigo} no encontrado");
}
