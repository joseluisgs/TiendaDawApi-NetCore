using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TiendaApi.Apis.Services.Email;

/// <summary>
/// Implementación de <see cref="IEmailService"/> diseñada específicamente para entornos
/// de desarrollo y pruebas.
/// 
/// <para>Esta implementación no envía emails reales a través de un servidor SMTP,
/// sino que registra todos los detalles del email en los logs de la aplicación.
/// Esto permite desarrollar y probar funcionalidades de email sin necesidad de
/// configurar un servidor SMTP real.</para>
/// 
/// <remarks>
/// <para><b>Características:</b></para>
/// <list type="bullet">
///   <item><description>Logs completos del contenido del email (para debugging).</description></item>
///   <item><description>No requiere configuración de SMTP ni credenciales.</description></item>
///   <item><description>Identifica claramente emails encolados vs enviados.</description></item>
///   <item><description>Útil para entornos de CI/CD y pruebas automatizadas.</description></item>
/// </list>
/// 
/// <para><b>Flujo de trabajo en desarrollo:</b></para>
/// <list type="number">
///   <item><description>El código de la aplicación crea un <see cref="EmailMessage"/>.</description></item>
///   <item><description>Llama a <see cref="SendEmailAsync"/> o <see cref="EnqueueEmailAsync"/>.</description></item>
///   <item><description>El servicio registra el email en los logs de aplicación.</description></item>
///   <item><description>El desarrollador revisa los logs para verificar el contenido.</description></item>
/// </list>
/// 
/// <para><b>Ejemplo de salida en logs:</b></para>
/// <code>
/// === EMAIL SENT ===
/// Para: usuario@ejemplo.com
/// Asunto: Confirmación de registro
/// Tipo: HTML
/// Cuerpo: &lt;h1&gt;Bienvenido...&lt;/h1&gt;
/// ======================
/// </code>
/// 
/// <para><b>Transición a producción:</b></para>
/// Para entornos de producción, reemplace esta implementación con una que utilice
/// un proveedor SMTP real (SmtpClient, SendGrid, Mailgun, etc.).
/// 
/// <example>
/// <para>Configuración en Program.cs para desarrollo:</para>
/// <code>
/// // En desarrollo, usar servicio que solo loguea
/// services.AddSingleton&lt;IEmailService, MemoryEmailService&gt;();
/// 
/// // En producción, usar servicio SMTP real
/// if (!env.IsDevelopment())
/// {
///     services.AddSingleton&lt;IEmailService, SmtpEmailService&gt;();
/// }
/// </code>
/// </example>
/// </remarks>
public class MemoryEmailService : IEmailService
{
    private readonly ILogger<MemoryEmailService> _logger;

    /// <summary>
    /// Constructor del servicio de email en memoria.
    /// </summary>
    /// <param name="logger">Instancia del logger para registrar los emails.</param>
    /// 
    /// <remarks>
    /// El logger debe estar configurado para mostrar mensajes de nivel Information
    /// o superior para visualizar los emails en la consola o archivo de logs.
    /// </remarks>
    public MemoryEmailService(ILogger<MemoryEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Encola un email para procesamiento asíncrono.
    /// En esta implementación de desarrollo, simplemente registra el email
    /// con el estado "ENQUEUED" para indicar que fue agregado a la cola.
    /// </summary>
    /// <param name="message">Objeto EmailMessage con los detalles del email.</param>
    /// <returns>Tarea completada inmediatamente.</returns>
    /// 
    /// <remarks>
    /// <para><b>Comportamiento:</b> A diferencia de una implementación de producción
    /// que realmente añadiría el email a una cola (RabbitMQ, Azure Service Bus, etc.),
    /// esta implementación solo registra el email como si hubiera sido encolado.</para>
    /// 
    /// <para><b>Diferencia con SendEmailAsync:</b> Ambos métodos funcionan de forma
    /// similar en esta implementación, la diferencia es el estado registrado
    /// (ENQUEUED vs SENT) para facilitar el debugging.</para>
    /// 
    /// <example>
    /// <code>
    /// var email = new EmailMessage
    /// {
    ///     To = "test@ejemplo.com",
    ///     Subject = "Newsletter",
    ///     Body = "Contenido..."
    /// };
    /// 
    /// await _emailService.EnqueueEmailAsync(email);
    /// // Output en logs:
    /// // === EMAIL ENQUEUED ===
    /// // Para: test@ejemplo.com
    /// // ...
    /// </code>
    /// </example>
    /// </remarks>
    public Task EnqueueEmailAsync(EmailMessage message)
    {
        LogEmail(message, "ENQUEUED");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Envía un email de forma inmediata.
    /// En esta implementación de desarrollo, registra el email
    /// con el estado "SENT" para simular el envío real.
    /// </summary>
    /// <param name="message">Objeto EmailMessage con los detalles del email.</param>
    /// <returns>Tarea completada inmediatamente.</returns>
    /// 
    /// <remarks>
    /// <para><b>Comportamiento:</b> En lugar de conectarse a un servidor SMTP
    /// y transmitir el email, este método registra todos los detalles del email
    /// en el sistema de logs de la aplicación.</para>
    /// 
    /// <para><b>Niveles de log:</b></para>
    /// <list type="bullet">
    ///   <item><description>Información básica (To, Subject, Tipo): Nivel Information.</description></item>
    ///   <item><description>Cuerpo del email: Nivel Debug (más detallado).</description></item>
    /// </list>
    /// 
    /// <para><b>Por qué separar en niveles:</b> El cuerpo puede ser muy extenso
    /// (especialmente emails HTML con imágenes), por lo que se registra a nivel
    /// Debug para no saturar los logs de producción/desa</para>
    /// 
    /// <example>
    /// <code>
    /// var email = new EmailMessage
    /// {
    ///     To = "cliente@tienda.com",
    ///     Subject = "Pedido Confirmado #12345",
    ///     Body = "&lt;html&gt;...contenido completo...&lt;/html&gt;",
    ///     IsHtml = true
    /// };
    /// 
    /// await _emailService.SendEmailAsync(email);
    /// 
    /// // En la consola/archivo de logs aparece:
    /// // === EMAIL SENT ===
    /// // Para: cliente@tienda.com
    /// // Asunto: Pedido Confirmado #12345
    /// // Tipo: HTML
    /// // ======================
    /// </code>
    /// </example>
    /// </remarks>
    public Task SendEmailAsync(EmailMessage message)
    {
        LogEmail(message, "SENT");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Método interno que registra los detalles del email en el logger.
    /// </summary>
    /// <param name="message">EmailMessage con el contenido a registrar.</param>
    /// <param name="status">Texto que indica el estado (ENQUEUED o SENT).</param>
    /// 
    /// <remarks>
    /// <para><b>Formato de salida:</b></para>
    /// <code>
/// === EMAIL [STATUS] ===
/// Para: {To}
/// Asunto: {Subject}
/// Tipo: {Tipo}
/// Cuerpo: {Body}
/// ======================
/// </code>
    /// 
    /// <para><b>Niveles de log:</b> El cuerpo del email se registra a nivel Debug
    /// porque puede ser muy extenso y no es necesario en los logs de producción.</para>
    /// </remarks>
    private void LogEmail(EmailMessage message, string status)
    {
        _logger.LogInformation("=== EMAIL {Status} ===", status);
        _logger.LogInformation("Para: {To}", message.To);
        _logger.LogInformation("Asunto: {Subject}", message.Subject);
        _logger.LogInformation("Tipo: {Type}", message.IsHtml ? "HTML" : "Texto plano");
        _logger.LogDebug("Cuerpo: {Body}", message.Body);
        _logger.LogInformation("======================");
    }
}
