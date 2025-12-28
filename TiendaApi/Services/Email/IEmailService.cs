namespace TiendaApi.Services.Email;

/// <summary>
/// Email message model for queuing
/// </summary>
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}

/// <summary>
/// Interface for email service
/// Java Spring equivalent: JavaMailSender
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email asynchronously
    /// </summary>
    Task SendEmailAsync(EmailMessage message);
    
    /// <summary>
    /// Queue email for background processing
    /// </summary>
    Task EnqueueEmailAsync(EmailMessage message);
}
