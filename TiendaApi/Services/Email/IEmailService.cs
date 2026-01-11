namespace TiendaApi.Services.Email;

/// <summary>
/// Modelo de mensaje de email para encolar.
/// </summary>
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}

/// <summary>
/// Interfaz del servicio de email.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un email de forma asíncrona.
    /// Returns: Task completada
    /// </summary>
    Task SendEmailAsync(EmailMessage message);
    
    /// <summary>
    /// Encola un email para procesamiento en segundo plano.
    /// Returns: Task completada
    /// </summary>
    Task EnqueueEmailAsync(EmailMessage message);
}
