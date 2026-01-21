using System.Threading.Channels;

namespace TiendaApi.Api.Services.Email;

/// <summary>
/// Servicio en segundo plano para procesar la cola de emails.
/// Usa Channel para gestión thread-safe de la cola.
/// </summary>
public class EmailBackgroundService(
    Channel<EmailMessage> emailChannel,
    IServiceProvider serviceProvider,
    ILogger<EmailBackgroundService> logger
) : BackgroundService
{
    private readonly Channel<EmailMessage> _emailChannel = emailChannel;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<EmailBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de email en segundo plano iniciado");

        await foreach (var emailMessage in _emailChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                _logger.LogInformation("Procesando email de la cola para: {To}", emailMessage.To);

                await emailService.SendEmailAsync(emailMessage);

                _logger.LogInformation("Email procesado exitosamente para: {To}", emailMessage.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando email para: {To}", emailMessage.To);
            }
        }

        _logger.LogInformation("Servicio de email en segundo plano detenido");
    }
}
