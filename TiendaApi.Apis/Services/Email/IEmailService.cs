using System.Threading.Tasks;

namespace TiendaApi.Apis.Services.Email;

/// <summary>
/// Representa un mensaje de correo electrónico que será enviado a través del servicio de email.
/// Esta clase encapsula toda la información necesaria para construir y enviar un email.
/// </summary>
/// 
/// <remarks>
/// <para><b>Propósito:</b> Serve como modelo de datos para la cola de emails,
/// permitiendo la construcción de mensajes de forma desacoplada del mecanismo de envío.</para>
/// 
/// <para><b>Ciclo de vida:</b></para>
/// <list type="number">
///   <item><description>Crear instancia con los datos del destinatario, asunto y cuerpo.</description></item>
///   <item><description>Establecer si el cuerpo es HTML o texto plano.</description></item>
///   <item><description>Pasar al servicio de email para envío o cola.</description></item>
/// </list>
/// 
/// <para><b>Validación:</b> Los campos To, Subject y Body son requeridos.
/// Se recomienda validar estos campos antes de crear la instancia.</para>
/// </remarks>
/// 
/// <example>
/// <para>Creación de un email HTML:</para>
/// <code>
/// var email = new EmailMessage
/// {
///     To = "usuario@ejemplo.com",
///     Subject = "Confirmación de registro",
///     Body = @"&lt;h1&gt;Bienvenido&lt;/h1&gt;
///               &lt;p&gt;Gracias por registrarte en nuestra plataforma.&lt;/p&gt;",
///     IsHtml = true
/// };
/// 
/// await _emailService.SendEmailAsync(email);
/// </code>
/// </example>
public class EmailMessage
{
    /// <summary>
    /// Dirección de correo electrónico del destinatario.
    /// Debe ser una dirección válida según el formato estándar de email.
    /// </summary>
    /// <remarks>
    /// <para><b>Formatos válidos:</b></para>
    /// <list type="bullet">
    ///   <item><description>Simple: usuario@dominio.com</description></item>
    ///   <item><description>Con nombre: Juan Pérez &lt;juan@dominio.com&gt;</description></item>
    ///   <item><description>Múltiples destinatarios: Separados por coma</description></item>
    /// </list>
    /// 
    /// <para><b>Validación:</b> Se recomienda validar el formato del email
    /// antes de crear el mensaje para evitar errores en el envío.</para>
    /// </remarks>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Asunto o título del correo electrónico.
    /// Aparece en la línea de asunto del email recibido.
    /// </summary>
    /// <remarks>
    /// <para><b>Recomendaciones:</b></para>
    /// <list type="bullet">
    ///   <item><description>Máximo 78 caracteres para mejor compatibilidad.</description></item>
    ///   <item><description>Evitar caracteres especiales que puedan causar problemas de codificación.</description></item>
    ///   <item><description>Ser descriptivo para facilitar la identificación del email.</description></item>
    /// </list>
    /// </remarks>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Cuerpo o contenido del correo electrónico.
    /// Puede ser texto plano o HTML según la propiedad <see cref="IsHtml"/> .
    /// </summary>
    /// <remarks>
    /// <para><b>Para texto plano:</b></para>
    /// <code>
    /// Body = "Estimado usuario,\n\nSu pedido ha sido confirmado.\n\nSaludos."
    /// </code>
    /// 
    /// <para><b>Para HTML:</b></para>
    /// <code>
/// Body = @"&lt;html&gt;
///           &lt;body&gt;
///             &lt;h1&gt;Confirmación de Pedido&lt;/h1&gt;
///             &lt;p&gt;Su pedido #12345 ha sido confirmado.&lt;/p&gt;
///           &lt;/body&gt;
///         &lt;/html&gt;";
///     IsHtml = true;
/// </code>
    /// 
    /// <para><b>Consideraciones de seguridad:</b></para>
    /// Si permite contenido HTML proporcionado por usuarios,
    /// sanitice el contenido para prevenir ataques XSS.
    /// </remarks>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el cuerpo del email está formateado en HTML.
    /// </summary>
    /// <value>
    /// <c>true</c> si el contenido de <see cref="Body"/> es HTML; <c>false</c> si es texto plano.
    /// </value>
    /// 
    /// <remarks>
    /// <para><b>Uso típico:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>true</c>: Newsletters, emails con formato, plantillas.</description></item>
    ///   <item><description><c>false</c>: Notificaciones simples, emails transaccionales básicos.</description></item>
    /// </list>
    /// 
    /// <para><b>Accesibilidad:</b> Considere enviar una versión alternativa en texto plano
    /// para clientes de correo que no soportan HTML o tienen HTML deshabilitado.</para>
    /// </remarks>
    public bool IsHtml { get; set; } = true;
}

/// <summary>
/// Define el contrato para el servicio de envío de correo electrónico.
/// Proporciona métodos para enviar emails de forma sincrónica y asíncrona,
/// así como para encolar emails para procesamiento en segundo plano.
/// 
/// <para>Esta interfaz está diseñada para soportar diferentes implementaciones,
/// desde servicios SMTP reales hasta servicios de desarrollo que simplemente
/// registran los emails en logs.</para>
/// 
/// <remarks>
/// <para><b>Patrón de implementación:</b> Se utiliza el patrón de abstracción
/// permitiendo intercambiar implementaciones sin modificar el código cliente.</para>
/// 
/// <para><b>Implementaciones disponibles:</b></para>
/// <list type="bullet">
///   <item><description><see cref="MemoryEmailService"/>: Desarrollo y pruebas (logs en consola).</description></item>
///   <item><description>SmtpEmailService: Producción (envío real via SMTP).</description></item>
///   <item><description>SendGridEmailService: Proveedor externo SendGrid.</description></item>
/// </list>
/// 
/// <para><b>Casos de uso comunes:</b></para>
/// <list type="number">
///   <item><description>Confirmación de registro de usuario.</description></item>
///   <item><description>Restablecimiento de contraseña.</description></item>
///   <item><description>Notificaciones de pedidos.</description></item>
///   <item><description>Newsletters y comunicaciones masivas.</description></item>
/// </list>
/// 
/// <para><b>Consideraciones de rendimiento:</b></para>
/// <list type="bullet">
///   <item><description>Para envíos masivos, utilice <see cref="EnqueueEmailAsync"/>.</description></item>
///   <item><description>El envío directo (<see cref="SendEmailAsync"/>) bloquea hasta completar.</description></item>
///   <item><description>Considere límites de tasa de su proveedor SMTP.</description></item>
/// </list>
/// </remarks>
/// 
/// <example>
/// <para>Uso básico en un servicio de usuario:</para>
/// <code>
/// public class UsuarioService
/// {
///     private readonly IEmailService _emailService;
///     
///     public async Task&lt;bool&gt; RegistrarUsuarioAsync(UsuarioDto dto)
///     {
///         // Crear usuario...
///         
///         // Enviar email de bienvenida
///         var email = new EmailMessage
///         {
///             To = dto.Email,
///             Subject = "Bienvenido a TiendaDaw",
///             Body = $"&lt;h1&gt;Hola {dto.Nombre}&lt;/h1&gt;...",
///             IsHtml = true
///         };
///         
///         await _emailService.SendEmailAsync(email);
///         return true;
///     }
/// }
/// </code>
/// </example>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo electrónico de forma asíncrona de manera inmediata.
    /// Este método espera a que el email sea enviado antes de completar la tarea.
    /// </summary>
    /// <param name="message">
    /// Objeto <see cref="EmailMessage"/> que contiene los detalles del email a enviar.
    /// No debe ser null. Las propiedades To, Subject y Body deben tener valores válidos.
    /// </param>
    /// <returns>
    /// Tarea asíncrona que se completa cuando el email ha sido enviado exitosamente
    /// o cuando se ha registrado el intento de envío (en modo desarrollo).
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item><description>El email se envía inmediatamente, sin cola de espera.</description></item>
    ///   <item><description>La tarea no se completa hasta que el servidor SMTP acepta el email.</description></item>
    ///   <item><description>En desarrollo, simplemente registra el email en los logs.</description></item>
    /// </list>
    /// 
    /// <para><b>Cuándo usar:</b></para>
    /// <list type="bullet">
    ///   <item><description>Emails transaccionales críticos (confirmación de compra).</description></item>
    ///   <item><description>Notificaciones que el usuario espera recibir inmediatamente.</description></item>
    ///   <item><description>Operaciones donde el usuario espera confirmación visual.</description></item>
    /// </list>
    /// 
    /// <para><b>Cuándo NO usar:</b></para>
    /// <list type="bullet">
    ///   <item><description>Emails masivos o newsletters (use EnqueueEmailAsync).</description></item>
    ///   <item><description>Cuando no necesita esperar confirmación de envío.</description></item>
    /// </list>
    /// 
    /// <para><b>Manejo de errores:</b></para>
    /// En producción, las excepciones de SMTP se propagan al llamador.
    /// En desarrollo, los errores se registran pero no impiden la завершение de la tarea.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Envío inmediato de email de confirmación
    /// var email = new EmailMessage
    /// {
    ///     To = "cliente@ejemplo.com",
    ///     Subject = "Pedido Confirmado",
    ///     Body = $"Su pedido #{pedido.Id} ha sido confirmado.",
    ///     IsHtml = true
    /// };
    /// 
    /// await _emailService.SendEmailAsync(email);
    /// Console.WriteLine("Email enviado exitosamente");
    /// </code>
    /// </example>
    Task SendEmailAsync(EmailMessage message);

    /// <summary>
    /// Encola un correo electrónico para procesamiento asíncrono en segundo plano.
    /// El email no se envía inmediatamente, sino que se añade a una cola de procesamiento.
    /// </summary>
    /// <param name="message">
    /// Objeto <see cref="EmailMessage"/> con los detalles del email a encolar.
    /// </param>
    /// <returns>
    /// Tarea asíncrona que se completa inmediatamente después de encolar el email.
    /// No espera a que el email sea enviado.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Patrón de arquitectura:</b> Utiliza el patrón Command Queue
    /// para desacoplar la creación del email de su envío efectivo.</para>
    /// 
    /// <para><b>Comportamiento:</b></para>
    /// <list type="bullet">
    ///   <item><description>La tarea se completa apenas el email se añade a la cola.</description></item>
    ///   <item><description>El envío real ocurre en segundo plano (background worker).</description></item>
    ///   <item><description>Ideal para operaciones que no requieren confirmación inmediata.</description></item>
    /// </list>
    /// 
    /// <para><b>Ventajas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Mejora el tiempo de respuesta de la aplicación.</description></item>
    ///   <item><description>Permite procesamiento por lotes de emails.</description></item>
    ///   <item><description>Maneja reintentos automáticos en caso de fallo temporal.</description></item>
    /// </list>
    /// 
    /// <para><b>Cuándo usar:</b></para>
    /// <list type="bullet">
    ///   <item><description>Newsletters a múltiples destinatarios.</description></item>
    ///   <item><description>Notificaciones no críticas.</description></item>
    ///   <item><description>Emails de marketing y promociones.</description></item>
    ///   <item><description>Cuando el rendimiento es prioritario sobre la inmediatez.</description></item>
    /// </list>
    /// 
    /// <para><b>Implementación típica:</b></para>
    /// Un background worker procesa la cola, enviando emails en intervalos
    /// configurables y manejando errores con reintentos exponenciales.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Encolar email para envío posterior
    /// var email = new EmailMessage
    /// {
    ///     To = "suscriptor@newsletter.com",
    ///     Subject = "Últimas ofertas de la semana",
    ///     Body = ObtenerPlantillaNewsletter(),
    ///     IsHtml = true
    /// };
    /// 
    /// await _emailService.EnqueueEmailAsync(email);
    /// Console.WriteLine("Email encolado para envío posterior");
    /// // El usuario continúa sin esperar el envío
    /// </code>
    /// </example>
    Task EnqueueEmailAsync(EmailMessage message);
}
