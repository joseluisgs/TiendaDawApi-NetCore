using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using System;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Storage;

/// <summary>
/// Interfaz que define el contrato para el servicio de almacenamiento de archivos.
/// Proporciona operaciones para guardar, eliminar y verificar archivos en el sistema
/// de almacenamiento de la aplicación.
/// 
/// <para>Esta abstracción permite implementar diferentes mecanismos de almacenamiento:</para>
/// <list type="bullet">
///   <item><description>Sistema de archivos local.</description></item>
///   <item><description>Servidor FTP/SFTP.</description></item>
///   <item><description>Servicios de almacenamiento en la nube (Azure Blob, AWS S3).</description></item>
///   <item><description>Almacenamiento distribuido.</description></item>
/// </list>
/// 
/// <remarks>
/// <para><b>Patrón de uso:</b></para>
/// <code>
/// 1. Recibir archivo desde formulario (IFormFile)
/// 2. Generar nombre único para el archivo
/// 3. Llamar SaveFileAsync para guardar
/// 4. Almacenar ruta relativa devuelta en base de datos
/// 5. Para eliminar, usar DeleteFileAsync con la ruta almacenada
/// </code>
/// 
/// <para><b>Seguridad:</b></para>
/// <list type="bullet">
///   <item><description>Validar tipos de archivo permitidos antes de guardar.</description></item>
///   <item><description>Renombrar archivos con nombres únicos (GUID) para prevenir colisiones.</description></item>
///   <item><description>No confiar en el nombre de archivo proporcionado por el usuario.</description></item>
///   <item><description>Limitar tamaño máximo de archivo.</description></item>
/// </list>
/// 
/// <para><b>Estructura de carpetas recomendada:</b></para>
/// <code>
/// storage/
/// ├── productos/          # Imágenes de productos
/// ├── usuarios/           # Avatares y documentos de usuarios
/// ├── categorias/         # Imágenes de categorías
/// └── documentos/         # Documentos varios (facturas, contratos)
/// </code>
/// 
/// <example>
/// <para>Uso en un controlador para subir imágenes de productos:</para>
/// <code>
/// [HttpPost("productos/{id}/imagen")]
/// public async Task&lt;ActionResult&gt; SubirImagen(int id, IFormFile archivo)
/// {
///     var resultado = await _storageService.SaveFileAsync(archivo, "productos");
///     
///     if (resultado.IsFailure)
///         return BadRequest(resultado.Error);
///     
///     // Actualizar producto con la ruta de la imagen
///     await _productoService.ActualizarImagenAsync(id, resultado.Value);
///     
///     return Ok(new { imagen = resultado.Value });
/// }
/// </code>
/// </example>
public interface IStorageService
{
    /// <summary>
    /// Guarda un archivo en el sistema de almacenamiento y devuelve la ruta relativa
    /// donde fue almacenado. Esta ruta es segura para almacenar en la base de datos.
    /// </summary>
    /// <param name="file">
    /// Objeto <see cref="IFormFile"/> que representa el archivo a guardar.
    /// No debe ser null y debe tener contenido (Length > 0).
    /// </param>
    /// <param name="folder">
    /// Carpeta destino dentro del almacenamiento donde se guardará el archivo.
    /// Ejemplos: "productos", "usuarios", "categorias", "documentos".
    /// </param>
    /// <returns>
    /// Result que contiene la ruta relativa del archivo guardado si tiene éxito,
    /// o un <see cref="DomainError"/> si ocurre algún error.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Proceso de guardado:</b></para>
    /// <list type="number">
    ///   <item><description>Validar que el archivo no sea null y tenga contenido.</description></item>
    ///   <item><description>Generar nombre único (GUID + extensión original).</description></item>
    ///   <item><description>Crear directorio de destino si no existe.</description></item>
    ///   <item><description>Copiar el archivo al destino.</description></item>
    ///   <item><description>Devolver ruta relativa para almacenar en BD.</description></item>
    /// </list>
    /// 
    /// <para><b>Generación de nombre de archivo:</b></para>
    /// Se utiliza un GUID para garantizar unicidad, combinado con la extensión original:
    /// <code>abc12345-6789-4def-1234-56789abcdef0.jpg</code>
    /// 
    /// <para><b>Errores comunes:</b></para>
    /// <list type="bullet">
    ///   <item><description>Archivo vacío o null.</description></item>
    ///   <item><description>Error de permisos en la carpeta de destino.</description></item>
    ///   <item><description>Espacio en disco insuficiente.</description></item>
    ///   <item><description>Tipo de archivo no permitido.</description></item>
    /// </list>
    /// 
    /// <para><b>Validación recomendada antes de llamar:</b></para>
    /// <code>
    /// if (archivo == null || archivo.Length == 0)
    ///     return BadRequest("Archivo vacío");
    /// 
    /// var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
    /// if (!extension.Match(".jpg", ".jpeg", ".png", ".webp"))
    ///     return BadRequest("Tipo de imagen no permitido");
    /// </code>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Guardar imagen de perfil de usuario
    /// var resultado = await _storageService.SaveFileAsync(avatar, "usuarios");
    /// 
    /// if (resultado.IsSuccess)
    /// {
    ///     var rutaBd = resultado.Value; // "/images/usuarios/abc123...jpg"
    ///     await _usuarioService.ActualizarAvatarAsync(usuarioId, rutaBd);
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Error: {resultado.Error}");
    /// }
    /// </code>
    /// </example>
    Task<Result<string, DomainError>> SaveFileAsync(IFormFile file, string folder);

    /// <summary>
    /// Elimina un archivo del sistema de almacenamiento.
    /// </summary>
    /// <param name="filename">
    /// Ruta relativa del archivo a eliminar. Esta ruta debe haber sido devuelta
    /// por <see cref="SaveFileAsync"/> o construida con <see cref="GetRelativePath(string, string)"/>
    /// </param>
    /// <returns>
    /// Result que contiene true si el archivo fue eliminado exitosamente,
    /// o un <see cref="DomainError"/> si ocurrió algún problema.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si el archivo no existe, devuelve success con false.</description></item>
    ///   <item><description>Si el archivo existe, lo elimina y devuelve success con true.</description></item>
    ///   <item><description>Los errores de permisos se devuelven como DomainError.</description></item>
    /// </list>
    /// 
    /// <para><b>Ejemplo de filename:</b></para>
    /// <code>
    /// // Rutas relativas válidas:
    /// filename = "/images/productos/uuid.jpg"
    /// filename = "images/usuarios/avatar.png"
    /// </code>
    /// 
    /// <para><b>Precaución:</b> Esta operación es irreversible. Verifique que el
    /// archivo realmente debe ser eliminado antes de llamar a este método.</para>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Eliminar imagen de producto al actualizar
    /// var resultado = await _storageService.DeleteFileAsync(producto.ImagenUrl);
    /// 
    /// if (resultado.IsSuccess &amp;&amp; resultado.Value)
    /// {
    ///     Console.WriteLine("Imagen anterior eliminada");
    /// }
    /// </code>
    /// </example>
    Task<Result<bool, DomainError>> DeleteFileAsync(string filename);

    /// <summary>
    /// Verifica si un archivo existe en el sistema de almacenamiento.
    /// </summary>
    /// <param name="filename">Ruta relativa o nombre del archivo a verificar.</param>
    /// <returns>
    /// <c>true</c> si el archivo existe y es accesible;
    /// <c>false</c> si el archivo no existe o no se puede acceder.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Uso común:</b></para>
    /// <list type="bullet">
    ///   <item><description>Verificar si un archivo existe antes de servirlo.</description></item>
    ///   <item><description>Validar que la imagen referenciada en BD aún existe.</description></item>
    ///   <item><description>Comprobar disponibilidad antes de eliminar.</description></item>
    /// </list>
    /// 
    /// <para><b>Ejemplo:</b></para>
    /// <code>
    /// if (_storageService.FileExists(producto.ImagenUrl))
    /// {
    ///     return File(_storageService.GetFullPath(producto.ImagenUrl), "image/jpeg");
    /// }
    /// else
    /// {
    ///     return NotFound("Imagen no encontrada");
    /// }
    /// </code>
    /// </remarks>
    bool FileExists(string filename);

    /// <summary>
    /// Obtiene la ruta física completa de un archivo en el sistema de archivos.
    /// Útil para operaciones que requieren la ruta absoluta (como servir archivos).
    /// </summary>
    /// <param name="filename">Nombre del archivo o ruta relativa.</param>
    /// <returns>
    /// Ruta completa del archivo en el sistema de archivos del servidor.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Ejemplo de retorno:</b></para>
    /// <code>
    /// GetFullPath("productos/uuid.jpg")
    /// // Retorna: "C:\inetpub\wwwroot\storage\productos\uuid.jpg"
    /// // o: "/var/www/storage/productos/uuid.jpg"
    /// </code>
    /// 
    /// <para><b>Uso típico:</b></para>
    /// <list type="bullet">
    ///   <item><description>Pasar a middleware de archivos estáticos.</description></item>
    ///   <item><description>Leer contenido del archivo.</description></item>
    ///   <item><description>Stream de archivos para descarga.</description></item>
    /// </list>
    /// 
    /// <para><b>Nota:</b> Esta ruta es específica del servidor donde se ejecuta
    /// la aplicación y no debe almacenarse en la base de datos.</para>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Servir archivo de imagen
    /// var rutaCompleta = _storageService.GetFullPath(imagenUrl);
    /// var stream = new FileStream(rutaCompleta, FileMode.Open);
    /// return File(stream, "image/jpeg");
    /// </code>
    /// </example>
    string GetFullPath(string filename);

    /// <summary>
    /// Genera una ruta relativa para almacenar en la base de datos.
    /// Combina el nombre del archivo con la carpeta especificada en un formato estandarizado.
    /// </summary>
    /// <param name="filename">Nombre del archivo (preferiblemente generado por SaveFileAsync).</param>
    /// <param name="folder">
    /// Carpeta dentro del storage donde se encuentra el archivo.
    /// Valor predeterminado: "productos".
    /// </param>
    /// <returns>
    /// Ruta relativa formateada para almacenar en base de datos.
    /// Formato: /images/{carpeta}/{nombre_archivo}
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Ejemplos de retorno:</b></para>
    /// <code>
    /// GetRelativePath("abc123.jpg", "productos")
    /// // Retorna: "/images/productos/abc123.jpg"
    /// 
    /// GetRelativePath("avatar.png", "usuarios")
    /// // Retorna: "/images/usuarios/avatar.png"
    /// 
    /// GetRelativePath("doc.pdf")  // carpeta por defecto: productos
    /// // Retorna: "/images/productos/doc.pdf"
    /// </code>
    /// 
    /// <para><b>Propósito:</b> Proporcionar una ruta estandarizada que:</para>
    /// <list type="bullet">
    ///   <item><description>Es independiente del sistema operativo del servidor.</description></item>
///       <item><description>Puede ser fácilmente convertida a ruta física con <see cref="GetFullPath"/>.</description></item>
///       <item><description>Es apropiada para almacenar en la base de datos.</description></item>
///       <item><description>Es utilizable como URL para acceder al archivo públicamente.</description></item>
///     </list>
///   </remarks>
/// 
///   <example>
///   <code>
///   // Generar ruta para almacenar en entidad
///   var rutaRelativa = _storageService.GetRelativePath("nuevo.jpg", "categorias");
///   
///   var categoria = new Categoria
///   {
///       Nombre = "Electrónica",
///       ImagenUrl = rutaRelativa  // "/images/categorias/nuevo.jpg"
///   };
///   
///   await _categoriaRepository.CreateAsync(categoria);
///   </code>
///   </example>
    string GetRelativePath(string filename, string folder = "productos");
}
