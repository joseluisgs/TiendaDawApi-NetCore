using System.IO;
using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;
using TiendaApi.Api.Infrastructures;



namespace TiendaApi.Tests.Unit.Infrastructures;

/// <summary>
/// Fase 6 — Tests educativos del pipeline de resiliencia (Polly v8).
/// Cubren los tres comportamientos clave: reintentos con backoff, corte de
/// reintentos al abrir el circuito y agotamiento total de intentos.
/// Los delays se anulan (delay: TimeSpan.Zero) para que los tests sean instantáneos.
/// </summary>
public class PollyConfigTests
{
    [Test]
    public void BuildEmailPipeline_DevuelvePipelineNoNulo()
    {
        var pipeline = PollyConfig.BuildEmailPipeline();

        pipeline.Should().NotBeNull();
    }

    [Test]
    public async Task Retry_FallaDosVeces_AlTercerIntento_Envia_YSonTresIntentos()
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(PollyConfig.EmailRetryOptions(delay: TimeSpan.Zero))
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        var attempts = 0;
        await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new IOException("SMTP caído");
            }

            return ValueTask.CompletedTask;
        }, CancellationToken.None);

        attempts.Should().Be(3, "2 fallos + 1 envío correcto en el 3er intento");
    }

    [Test]
    public async Task Retry_SiempreFalla_AgotaLos3Reintentos_YPropagaElUltimoError()
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(PollyConfig.EmailRetryOptions(delay: TimeSpan.Zero))
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        var attempts = 0;
        var act = async () => await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            throw new IOException("SMTP caído");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();

        attempts.Should().Be(4, "1 intento original + 3 reintentos = 4 intentos");
    }

    [Test]
    public async Task CircuitBreaker_TresFallosSeguidos_Abre_YLaCuartaLlamadaNoEjecutaElCallback()
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(PollyConfig.EmailCircuitBreakerOptions())
            .Build();

        var attempts = 0;
        var act = async () => await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            throw new IOException("SMTP caído");
        }, CancellationToken.None);

        for (var i = 0; i < 3; i++)
        {
            await act.Should().ThrowAsync<IOException>();
        }

        attempts.Should().Be(3, "el umbral del circuit breaker es 3 fallos");

        // Circuito abierto: se rechaza al instante SIN llegar a SMTP.
        await act.Should().ThrowAsync<BrokenCircuitException>();
        attempts.Should().Be(3, "BrokenCircuitException corta antes de ejecutar el callback");
    }

    [Test]
    public async Task PipelineCompleto_CuandoElCircuitoAbre_ElRetryNoInsiste()
    {
        // Mismo orden que BuildEmailPipeline: Retry → CircuitBreaker → Timeout.
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(PollyConfig.EmailRetryOptions(delay: TimeSpan.Zero))
            .AddCircuitBreaker(PollyConfig.EmailCircuitBreakerOptions())
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        var attempts = 0;
        var act = async () => await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            throw new IOException("SMTP caído");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<BrokenCircuitException>();

        // El circuito abre al 3er fallo; el 4to intento del retry se rechaza sin
        // volver a llamar (ShouldHandle excluye BrokenCircuitException).
        attempts.Should().Be(3);
    }
}
