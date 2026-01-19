using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TiendaApi.Apis.Services.Background.Jobs;

namespace TiendaApi.Apis.Services.Background.Host;

/// <summary>
/// Servicio de fondo que ejecuta tareas programadas.
/// Maneja la ejecución periódica de jobs según la configuración.
/// 
/// <para><b>Comportamiento:</b></para>
/// <list type="bullet">
///   <item><description>En desarrollo: Ejecuta cada 1 minuto</description></item>
///   <item><description>En producción: Ejecuta según configuración (por defecto cada 7 días)</description></item>
/// </list>
/// 
/// <para><b>Jobs disponibles:</b></para>
/// <list type="bullet">
///   <item><description>ProductoReportTask: Envía reportes de productos nuevos por email</description></item>
/// </list>
/// </summary>
public class BackgroundJobService(
    IServiceProvider serviceProvider,
    ILogger<BackgroundJobService> logger,
    IConfiguration configuration
) : BackgroundService
{
    private readonly bool _isDevelopment = configuration.GetValue<bool>("IsDevelopment");
    private readonly int _executionIntervalMinutes = configuration.GetValue<int>("Scheduler:ExecutionIntervalMinutes", 1);
    private readonly int _executionIntervalHours = configuration.GetValue<int>("Scheduler:ExecutionIntervalHours", 168);

    /// <summary>
    /// Ejecuta el loop principal del servicio de fondo.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BackgroundJobService iniciado - Modo: {Modo}", 
            _isDevelopment ? "DESARROLLO" : "PRODUCCION");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en BackgroundJobService");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        logger.LogInformation("BackgroundJobService detenido");
    }

    /// <summary>
    /// Ejecuta todos los jobs registrados.
    /// </summary>
    private async Task ExecuteJobsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var productoTask = scope.ServiceProvider.GetRequiredService<IProductoReportTask>();

        logger.LogDebug("Ejecutando jobs programados");

        await productoTask.ExecuteAsync();

        // Calcular siguiente ejecución
        var interval = _isDevelopment
            ? TimeSpan.FromMinutes(_executionIntervalMinutes)
            : TimeSpan.FromHours(_executionIntervalHours);

        logger.LogDebug("Siguiente ejecucion en {Interval} minutos", Math.Round(interval.TotalMinutes, 0));

        await Task.Delay(interval, ct);
    }
}
