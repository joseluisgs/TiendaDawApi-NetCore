using System.Threading.Tasks;

namespace TiendaApi.Api.Services.Email;

/// <summary>
/// Mensaje de correo electrónico.
/// </summary>
public class EmailMessage
{
    /// <summary>Destinatario.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Asunto.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Cuerpo del mensaje.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Indica si el cuerpo es HTML.</summary>
    public bool IsHtml { get; set; } = true;
}

/// <summary>
/// Contrato del servicio de email.
/// </summary>
public interface IEmailService
{
    /// <summary>Envía un email de forma inmediata.</summary>
    /// <param name="message">Mensaje a enviar.</param>
    Task SendEmailAsync(EmailMessage message);

    /// <summary>Encola un email para envío posterior.</summary>
    /// <param name="message">Mensaje a encolar.</param>
    Task EnqueueEmailAsync(EmailMessage message);
}
