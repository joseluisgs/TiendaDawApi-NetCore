namespace TiendaApi.Apis.Services.Email;

/// <summary>
/// Servicio de email para desarrollo local.
/// Encola emails y los loguea en la consola en lugar de enviarlos por SMTP.
/// Útil para desarrollo sin configurar SMTP real.
/// </summary>
public class MemoryEmailService : IEmailService
{
    private readonly ILogger<MemoryEmailService> _logger;

    public MemoryEmailService(ILogger<MemoryEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Encola un email para procesamiento.
    /// En desarrollo, simplemente loguea el email.
    /// </summary>
    public Task EnqueueEmailAsync(EmailMessage message)
    {
        LogEmail(message, "ENQUEUED");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Envía un email inmediatamente.
    /// En desarrollo, loguea el contenido en lugar de enviar.
    /// </summary>
    public Task SendEmailAsync(EmailMessage message)
    {
        LogEmail(message, "SENT");
        return Task.CompletedTask;
    }

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
