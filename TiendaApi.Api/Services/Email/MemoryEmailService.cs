using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TiendaApi.Api.Services.Email;

/// <summary>
/// Implementación de IEmailService para desarrollo que solo loguea los emails.
/// </summary>
public class MemoryEmailService : IEmailService
{
    private readonly ILogger<MemoryEmailService> _logger;

    public MemoryEmailService(ILogger<MemoryEmailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task EnqueueEmailAsync(EmailMessage message)
    {
        LogEmail(message, "ENQUEUED");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
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
