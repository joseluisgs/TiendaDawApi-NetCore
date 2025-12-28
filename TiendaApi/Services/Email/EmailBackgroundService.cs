using System.Threading.Channels;

namespace TiendaApi.Services.Email;

/// <summary>
/// Background service for processing email queue
/// Java Spring equivalent: @Scheduled or @Async method processing queue
/// Uses Channel for thread-safe queue management
/// </summary>
public class EmailBackgroundService : BackgroundService
{
    private readonly Channel<EmailMessage> _emailChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(
        Channel<EmailMessage> emailChannel,
        IServiceProvider serviceProvider,
        ILogger<EmailBackgroundService> logger)
    {
        _emailChannel = emailChannel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Background Service started");

        await foreach (var emailMessage in _emailChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Create a scope to resolve scoped services
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                _logger.LogInformation("Processing email from queue to: {To}", emailMessage.To);
                
                await emailService.SendEmailAsync(emailMessage);
                
                _logger.LogInformation("Email processed successfully to: {To}", emailMessage.To);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the background service
                _logger.LogError(ex, "Error processing email to: {To}", emailMessage.To);
            }
        }

        _logger.LogInformation("Email Background Service stopped");
    }
}
