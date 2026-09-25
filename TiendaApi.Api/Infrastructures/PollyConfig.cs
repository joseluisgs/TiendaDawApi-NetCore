using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Fase 6 — Políticas de resiliencia con Polly v8 (ResiliencePipeline) sobre el
/// único I/O externo real de la API: el envío de email por SMTP.
///
/// Comparación con el reintento "a mano" de <c>PedidosService.cs</c>
/// (const MaxRetries = 3, bucle for + try/catch + Task.Delay): aquí la lógica es
/// declarativa — backoff exponencial, cortacircuitos y timeout —, testeable por
/// separado y con logs en cada transición (Serilog vía ILogger).
///
/// Cadena (de fuera hacia dentro): Retry → CircuitBreaker → Timeout(10s por intento).
///
/// No aplica sobre BD/EF (EnableRetryOnFailure es incompatible con las
/// transacciones explícitas de PedidosService) ni sobre HttpClient (la API no
/// tiene llamadas salientes reales).
/// </summary>
public static class PollyConfig
{
    /// <summary>Timeout por intento: si SMTP no responde en 10s, se cancela y cuenta como fallo.</summary>
    public static readonly TimeSpan EmailTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Cuánto tiempo queda el circuito abierto tras saltar.</summary>
    public static readonly TimeSpan EmailBreakDuration = TimeSpan.FromSeconds(30);

    /// <summary>Reintentos: 3 (es decir, 1 intento original + 3 reintentos), backoff 2^n → 1s, 2s, 4s.</summary>
    public static RetryStrategyOptions EmailRetryOptions(ILogger? logger = null, TimeSpan? delay = null) => new()
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = delay ?? TimeSpan.FromSeconds(1),
        // No reintentar si el circuito ya está abierto (BrokenCircuit no se cura
        // repitiendo el intento) ni cancelaciones del hosting.
        ShouldHandle = new PredicateBuilder()
            .Handle<Exception>(ex => ex is not BrokenCircuitException and not OperationCanceledException),
        OnRetry = args =>
        {
            logger?.LogWarning(
                "Polly[email] reintento {Attempt}/3 tras fallo de SMTP (próximo intento en {Delay})",
                args.AttemptNumber,
                args.RetryDelay);
            return ValueTask.CompletedTask;
        }
    };

    /// <summary>Cortacircuitos: 3 fallos consecutivos (ratio 100%) en ventana de 30s → 30s sin llamar a SMTP.</summary>
    public static CircuitBreakerStrategyOptions EmailCircuitBreakerOptions(ILogger? logger = null) => new()
    {
        FailureRatio = 1.0,
        MinimumThroughput = 3,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = EmailBreakDuration,
        ShouldHandle = new PredicateBuilder()
            .Handle<Exception>(ex => ex is not OperationCanceledException),
        OnOpened = args =>
        {
            logger?.LogWarning(
                "Polly[email] circuito ABIERTO tras 3 fallos: {BreakDuration}s sin intentar SMTP",
                args.BreakDuration);
            return ValueTask.CompletedTask;
        },
        OnClosed = _ =>
        {
            logger?.LogInformation("Polly[email] circuito CERRADO: reanudando envíos");
            return ValueTask.CompletedTask;
        },
        OnHalfOpened = _ =>
        {
            logger?.LogInformation("Polly[email] circuito HALF-OPEN: enviando prueba");
            return ValueTask.CompletedTask;
        }
    };

    /// <summary>
    /// Pipeline completo del email: Retry (externo) → CircuitBreaker → Timeout(10s).
    /// Registrado como singleton en <see cref="EmailConfig.AddEmail"/>.
    /// </summary>
    public static ResiliencePipeline BuildEmailPipeline(ILogger? logger = null) =>
        new ResiliencePipelineBuilder()
            .AddRetry(EmailRetryOptions(logger))
            .AddCircuitBreaker(EmailCircuitBreakerOptions(logger))
            .AddTimeout(EmailTimeout)
            .Build();
}
