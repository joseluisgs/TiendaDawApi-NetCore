using System.Threading.Channels;
using MailKit.Net.Smtp;
using MimeKit;
using Polly;
using Polly.CircuitBreaker;
using TiendaApi.Api.Infrastructures;

namespace TiendaApi.Api.Services.Email;

/// <summary>
/// Servicio de email usando MailKit.
/// Envía emails a través de SMTP.
/// El envío va envuelto en el pipeline de resiliencia de
/// <see cref="PollyConfig"/> (Retry 3 + CircuitBreaker + Timeout 10s).
/// </summary>
public class MailKitEmailService(
    IConfiguration configuration,
    ILogger<MailKitEmailService> logger,
    Channel<EmailMessage> emailChannel,
    ResiliencePipeline? emailPipeline = null
) : IEmailService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<MailKitEmailService> _logger = logger;
    private readonly Channel<EmailMessage> _emailChannel = emailChannel;
    private readonly ResiliencePipeline _emailPipeline =
        emailPipeline ?? PollyConfig.BuildEmailPipeline(logger);

    /// <summary>
    /// Resuelve la configuración SMTP completa (host, puerto, credenciales y
    /// remitente). Extraído de SendEmailAsync para que ese método se quede con
    /// la política de envío (Polly) y el manejo de errores; null = falta
    /// configuración esencial y no hay nada con lo que conectar.
    /// </summary>
    private (string Host, int Port, string User, string Password, string FromEmail, string FromName)? ResolverConfigSmtp()
    {
        var host = _configuration["Smtp:Host"];
        var user = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:FromEmail"] ?? user;

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
            return null;

        return (
            host,
            int.Parse(_configuration["Smtp:Port"] ?? "587"),
            user,
            password,
            fromEmail,
            _configuration["Smtp:FromName"] ?? "TiendaApi");
    }

    /// <summary>
    /// Envía un email inmediatamente usando SMTP.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation/Internal)
    /// </summary>
    public async Task SendEmailAsync(EmailMessage message)
    {
        try
        {
            var smtp = ResolverConfigSmtp();
            if (smtp is null)
            {
                _logger.LogWarning("SMTP no configurado correctamente, omitiendo envío de email");
                return;
            }

            var mimeMessage = ConstruirMimeMessage(message, new MailboxAddress(smtp.Value.FromName, smtp.Value.FromEmail));

            // Polly: cada intento tiene Timeout(10s); hasta 3 reintentos con
            // backoff 2^n; si hay 3 fallos seguidos el CircuitBreaker abre 30s y el
            // envío se omite (BrokenCircuitException) sin agotar los reintentos.
            await _emailPipeline.ExecuteAsync(async _ =>
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(smtp.Value.Host, smtp.Value.Port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtp.Value.User, smtp.Value.Password);
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }, CancellationToken.None);

            _logger.LogInformation("Email enviado exitosamente a: {To}", message.To);
        }
        catch (BrokenCircuitException ex)
        {
            // CircuitBreaker abierto: no se reintenta más; solo se avisa.
            // El caller es EmailBackgroundService → el request HTTP nunca falla por email.
            _logger.LogWarning(ex, "Circuito de email abierto (3 fallos seguidos), envío omitido a: {To}", message.To);
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout de 10s de Polly agotado enviando email a: {To}", message.To);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a: {To}", message.To);
            throw;
        }
    }

    /// <summary>
    /// Construye el mensaje Mime a partir del EmailMessage de dominio (remitente,
    /// destinatario, asunto y cuerpo HTML/texto). Extraido de SendEmailAsync para
    /// que ese metodo se quede con la politica de envio (Polly) y el manejo de
    /// errores, sin la construccion intercalada.
    /// </summary>
    private static MimeMessage ConstruirMimeMessage(EmailMessage message, MailboxAddress remitente)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(remitente);
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

        return mimeMessage;
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
