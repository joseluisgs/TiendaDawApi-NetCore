using System.Threading.Channels;
using MailKit.Net.Smtp;
using MimeKit;

namespace TiendaApi.Services.Email;

/// <summary>
/// MailKit email service implementation
/// Java Spring equivalent: JavaMailSender implementation
/// Sends emails via SMTP using MailKit library
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailKitEmailService> _logger;
    private readonly Channel<EmailMessage> _emailChannel;

    public MailKitEmailService(
        IConfiguration configuration, 
        ILogger<MailKitEmailService> logger,
        Channel<EmailMessage> emailChannel)
    {
        _configuration = configuration;
        _logger = logger;
        _emailChannel = emailChannel;
    }

    /// <summary>
    /// Send email immediately using SMTP
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

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                _logger.LogWarning("SMTP not configured, skipping email send");
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

            _logger.LogInformation("Email sent successfully to: {To}", message.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {To}", message.To);
            throw;
        }
    }

    /// <summary>
    /// Queue email for background processing
    /// Non-blocking operation that adds to channel
    /// </summary>
    public async Task EnqueueEmailAsync(EmailMessage message)
    {
        try
        {
            await _emailChannel.Writer.WriteAsync(message);
            _logger.LogInformation("Email queued for background processing to: {To}", message.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email for: {To}", message.To);
        }
    }
}
