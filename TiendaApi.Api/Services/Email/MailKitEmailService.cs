using System.Threading.Channels;
using MailKit.Net.Smtp;
using MimeKit;

namespace TiendaApi.Api.Services.Email;

/// <summary>
/// Servicio de email usando MailKit.
/// Envía emails a través de SMTP.
/// </summary>
public class MailKitEmailService(
    IConfiguration configuration,
    ILogger<MailKitEmailService> logger,
    Channel<EmailMessage> emailChannel
) : IEmailService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<MailKitEmailService> _logger = logger;
    private readonly Channel<EmailMessage> _emailChannel = emailChannel;

    /// <summary>
    /// Envía un email inmediatamente usando SMTP.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation/Internal)
    /// </summary>
    public async Task SendEmailAsync(EmailMessage message)
    {
        try
        {
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var smtpUser = _configuration["Smtp:Username"];
            var smtpPassword = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Smtp:FromName"] ?? "TiendaApi";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogWarning("SMTP no configurado correctamente, omitiendo envío de email");
                return;
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(message.To));
            mimeMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder();
            if (message.IsHtml)
            {
                bodyBuilder.HtmlBody = message.Body;
            }
            else
            {
                bodyBuilder.TextBody = message.Body;
            }
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email enviado exitosamente a: {To}", message.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a: {To}", message.To);
            throw;
        }
    }

    /// <summary>
    /// Encola un email para procesamiento en segundo plano.
    /// Operación no bloqueante que añade al canal.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Internal)
    /// </summary>
    public async Task EnqueueEmailAsync(EmailMessage message)
    {
        try
        {
            await _emailChannel.Writer.WriteAsync(message);
            _logger.LogInformation("Email encolado para procesamiento en segundo plano a: {To}", message.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al encolar email para: {To}", message.To);
        }
    }
}
