namespace TiendaApi.Apis.Errors;

/// <summary>
/// Clase base abstracta para todos los errores del dominio.
/// 
/// <para>
/// Implementa el patrón Result para el manejo de errores sin usar excepciones.
/// Esto permite un flujo de código más predecible y testable.
/// </para>
/// 
/// <para>
/// <b>Patrón Result:</b> En lugar de lanzar excepciones, los métodos devuelven
/// Result (de CSharpFunctionalExtensions) donde el error es un DomainError.
/// Esto hace que el manejo de errores sea explícito y obligatorio.
/// </para>
/// 
/// <para>
/// <b>Ventajas sobre excepciones:</b>
/// <list type="bullet">
///   <item><description>El compilador obliga a manejar el error.</description></item>
///   <item><description>No hay excepciones no controladas.</description></item>
///   <item><description>Se puede encadenar con Map, Bind, Check, etc.</description></item>
///   <item><description>Mejor rendimiento (no hay stack trace).</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Ventajas sobre excepciones:</b>
/// <list type="bullet">
///   <item><description>El compilador obliga a manejar el error.</description></item>
///   <item><description>No hay excepciones no controladas.</description></item>
///   <item><description>Se puede encadenar con Map, Bind, Check, etc.</description></item>
///   <item><description>Mejor rendimiento (no hay stack trace).</description></item>
/// </list>
/// </para>
/// 
/// <example>
/// Uso típico del patrón Result:
/// <code>
/// var resultado = _service.FindByIdAsync(1);
/// resultado.Match(
///     onSuccess: usuario => Console.WriteLine(usuario.Nombre),
///     onFailure: error => Console.WriteLine($"Error: {error.Message}")
/// );
/// </code>
/// </example>
/// </summary>
/// <param name="Message">Mensaje descriptivo del error legible por humanos.</param>
public abstract record DomainError(string Message)
{
    /// <summary>
    /// Representación en string del error de dominio para logging y debugging.
    /// 
    /// <para>
    /// Formato: "NombreDelTipo: Mensaje del error"
    /// </para>
    /// <example>
    /// Console.WriteLine(error.ToString());
    /// // Salida: "NotFoundError: Usuario con ID 5 no encontrado"
    /// </example>
    /// </summary>
    /// <returns>String formateado con el tipo y mensaje del error.</returns>
    public override string ToString() => $"{GetType().Name}: {Message}";
}

#region Errores Base

/// <summary>
/// Error específico para recursos no encontrados.
/// 
/// <para>
/// Se usa cuando se intenta acceder a un recurso que no existe en la base de datos.
/// Ejemplos comunes: usuario con ID inexistente, producto eliminado, etc.
/// </para>
/// </summary>
/// <param name="Message">Mensaje descriptivo del error.</param>
public sealed record NotFoundError(string Message)
    : DomainError(Message)
{
    /// <summary>
    /// Factory method para crear un error "no encontrado" por ID.
    /// 
    /// <para>
    /// Genera un mensaje estandarizado: "Recurso con ID {id} no encontrado"
    /// </para>
    /// </summary>
    /// <param name="id">Identificador del recurso que no existe.</param>
    /// <param name="resourceType">Nombre del tipo de recurso para el mensaje (opcional).</param>
    /// <returns>NotFoundError listo para retornar.</returns>
    /// <example>
    /// return NotFoundError.FromId(5, "Usuario");
    /// // Genera: "Recurso con ID 5 no encontrado"
    /// </example>
    public static NotFoundError FromId(long id, string resourceType = "Unknown") =>
        new($"Recurso con ID {id} no encontrado");
}

/// <summary>
/// Error de validación de datos de entrada.
/// 
/// <para>
/// Se usa cuando los datos proporcionados no cumplen las reglas de validación.
/// Incluye un diccionario con los errores específicos por campo.
/// </para>
/// 
/// <para>
/// <b>Flujo de validación:</b>
/// <list type="number">
///   <item><description>Controller recibe DTO de request.</description></item>
///   <item><description>Validator (FluentValidation) verifica los datos.</description></item>
///   <item><description>Si hay errores, se crea ValidationError con detalles.</description></item>
///   <item><description>Controller retorna 400 Bad Request con errores por campo.</description></item>
/// </list>
/// </para>
/// </summary>
/// <param name="Message">Resumen general de los errores.</param>
/// <param name="ValidationErrors">
/// Diccionario donde la clave es el nombre del campo y el valor es un array de mensajes de error.
/// </param>
public sealed record ValidationError(string Message, Dictionary<string, string[]> ValidationErrors)
    : DomainError(Message)
{
    /// <summary>
    /// Factory method para crear un error de validación con errores por campo.
    /// </summary>
    /// <param name="fieldErrors">
    /// Diccionario con estructura: { "campo1" : ["error1", "error2"], "campo2" : ["error1"] }
    /// </param>
    /// <returns>ValidationError con todos los errores por campo.</returns>
    /// <example>
    /// var errores = new Dictionary&lt;string, string[]&gt;
    /// {
    ///     { "Email", new[] { "El email es obligatorio", "Formato inválido" } },
    ///     { "Password", new[] { "Mínimo 8 caracteres" } }
    /// };
    /// return ValidationError.WithFieldErrors(errores);
    /// </example>
    public static ValidationError WithFieldErrors(Dictionary<string, string[]> fieldErrors) =>
        new("Errores de validación", fieldErrors);

    /// <summary>
    /// Factory method para crear un error de validación simple sin detalles por campo.
    /// 
    /// <para>
    /// Útil cuando solo se necesita un mensaje de error general sin detalles específicos.
    /// </para>
    /// </summary>
    /// <param name="message">Mensaje de error general.</param>
    /// <returns>ValidationError con diccionario vacío de detalles.</returns>
    /// <example>
    /// return ValidationError.Create("El producto no cumple los requisitos");
    /// </example>
    public static ValidationError Create(string message) =>
        new(message, new Dictionary<string, string[]>());
}

/// <summary>
/// Error cuando se viola una regla de negocio.
/// 
/// <para>
/// A diferencia de ValidationError (que es sobre datos de entrada),
/// BusinessRuleError es sobre reglas específicas del dominio.
/// </para>
/// 
/// <para>
/// Ejemplos de reglas de negocio:
/// <list type="bullet">
///   <item><description>No se puede eliminar un pedido ya enviado.</description></item>
///   <item><description>El stock no puede ser negativo (sin backorder).</description></item>
///   <item><description>El email debe ser único por usuario.</description></item>
/// </list>
/// </para>
/// </summary>
/// <param name="Message">Descripción de la regla violada.</param>
public sealed record BusinessRuleError(string Message)
    : DomainError(Message) { }

/// <summary>
/// Error de autenticación (usuario no identificado).
/// 
/// <para>
/// Se usa cuando el usuario no ha proporcionado credenciales válidas
/// o el token JWT ha expirado o es inválido.
/// </para>
/// 
/// <para>
/// <b>Diferencia con ForbiddenError:</b>
/// <list type="bullet">
///   <item><term>UnauthorizedError</term>: No quién es el usuario (autenticación).</item>
///   <item><term>ForbiddenError</term>: No tiene permisos (autorización).</item>
/// </list>
/// </para>
/// </summary>
/// <param name="Message">Mensaje descriptivo del error de autenticación.</param>
public sealed record UnauthorizedError(string Message = "No autorizado")
    : DomainError(Message)
{
    /// <summary>
    /// Factory method para credenciales incorrectas.
    /// </summary>
    /// <returns>UnauthorizedError con mensaje de credenciales inválidas.</returns>
    public static UnauthorizedError InvalidCredentials() => new("Credenciales inválidas");

    /// <summary>
    /// Factory method para token expirado o inválido.
    /// </summary>
    /// <returns>UnauthorizedError con mensaje de token expirado.</returns>
    public static UnauthorizedError TokenExpired() => new("Token expirado o inválido");
}

/// <summary>
/// Error de autorización (acceso prohibido).
/// 
/// <para>
/// Se usa cuando el usuario está autenticado pero no tiene permisos
/// para realizar la acción solicitada.
/// </para>
/// 
/// <para>
/// <b>Ejemplos típicos:</b>
/// <list type="bullet">
///   <item><description>Usuario intentando acceder a recurso de otro usuario.</description></item>
///   <item><description>Usuario USER intentando acceder a endpoint de ADMIN.</description></item>
/// </list>
/// </para>
/// </summary>
/// <param name="Message">Mensaje descriptivo del error de autorización.</param>
public sealed record ForbiddenError(string Message = "Acceso denegado")
    : DomainError(Message)
{
    /// <summary>
    /// Factory method para crear un error cuando el usuario no es propietario.
    /// </summary>
    /// <param name="resourceType">Nombre del tipo de recurso (ej: "pedido", "producto").</param>
    /// <param name="resourceId">ID del recurso como string (para IDs no numéricos como ObjectId).</param>
    /// <returns>ForbiddenError con mensaje personalizado.</returns>
    /// <example>
    /// return ForbiddenError.NotOwner("pedido", "PED-12345");
    /// // Genera: "No tienes permisos para acceder a este pedido (ID: PED-12345)"
    /// </example>
    public static ForbiddenError NotOwner(string resourceType, string resourceId) =>
        new($"No tienes permisos para acceder a este {resourceType} (ID: {resourceId})");

    /// <summary>
    /// Factory method para crear un error cuando el usuario no es propietario.
    /// </summary>
    /// <param name="resourceType">Nombre del tipo de recurso (ej: "pedido", "producto").</param>
    /// <param name="resourceId">ID del recurso (opcional, para mensaje más específico).</param>
    /// <returns>ForbiddenError con mensaje personalizado.</returns>
    /// <example>
    /// return ForbiddenError.NotOwner("pedido", 123);
    /// // Genera: "No tienes permisos para acceder a este pedido (ID: 123)"
    /// </example>
    public static ForbiddenError NotOwner(string resourceType = "recurso", long? resourceId = null) =>
        new(resourceId != null
            ? $"No tienes permisos para acceder a este {resourceType} (ID: {resourceId})"
            : $"No tienes permisos para acceder a este {resourceType}");
}

/// <summary>
/// Error de conflicto de estado o recursos duplicados.
/// 
/// <para>
/// Se usa cuando la operación no se puede realizar debido a un conflicto
/// con el estado actual del recurso.
/// </para>
/// 
/// <para>
/// <b>Ejemplos:</b>
/// <list type="bullet">
///   <item><description>Crear usuario con email que ya existe.</description></item>
///   <item><description>Actualizar categoría con nombre de otra categoría.</description></item>
///   <item><description>Modificar recurso que fue modificado por otro proceso.</description></item>
/// </list>
/// </para>
/// </summary>
/// <param name="Message">Mensaje descriptivo del conflicto.</param>
public sealed record ConflictError(string Message)
    : DomainError(Message)
{
    /// <summary>
    /// Factory method para errores de recursos duplicados.
    /// </summary>
    /// <param name="resourceType">Tipo de recurso (ej: "usuario", "categoría").</param>
    /// <param name="value">Valor duplicado (ej: "admin@tienda.com").</param>
    /// <returns>ConflictError con mensaje de duplicado.</returns>
    /// <example>
    /// return ConflictError.Duplicate("email", "admin@tienda.com");
    /// // Genera: "Ya existe un email con el valor 'admin@tienda.com'"
    /// </example>
    public static ConflictError Duplicate(string resourceType, string value) =>
        new($"Ya existe un {resourceType} con el valor '{value}'");
}

/// <summary>
/// Error interno del servidor.
/// 
/// <para>
/// Se usa para errores inesperados que no deberían ocurrir en operación normal:
/// <list type="bullet">
///   <item><description>Errores de base de datos no esperados.</description></item>
///   <item><description>Fallo de servicios externos (Redis, SMTP).</description></item>
///   <item><description>Errores de programación (null references, etc.).</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Nota:</b> En producción, estos errores no deben mostrar detalles al cliente
/// por seguridad. Se loguea el error completo y se retorna un mensaje genérico.
/// </para>
/// </summary>
/// <param name="Message">Mensaje descriptivo (genérico para producción).</param>
public sealed record InternalError(string Message = "Error interno del servidor")
    : DomainError(Message) { }

#endregion
