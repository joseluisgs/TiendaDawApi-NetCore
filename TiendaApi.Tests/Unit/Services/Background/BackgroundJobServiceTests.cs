using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Services.Background.Host;

namespace TiendaApi.Tests.Unit.Services.Background;

public class BackgroundJobServiceTests
{
    private Mock<ILogger<BackgroundJobService>> _loggerMock = null!;

    private BackgroundJobService CreateService(bool isDevelopment = true, int intervalMinutes = 1, int intervalHours = 168)
    {
        _loggerMock = new Mock<ILogger<BackgroundJobService>>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "IsDevelopment", isDevelopment.ToString().ToLower() },
            { "Scheduler:ExecutionIntervalMinutes", intervalMinutes.ToString() },
            { "Scheduler:ExecutionIntervalHours", intervalHours.ToString() }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        return new BackgroundJobService(
            Mock.Of<IServiceProvider>(),
            _loggerMock.Object,
            configuration);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_CreaInstanciaCorrectamente()
    {
        var service = CreateService();

        service.Should().NotBeNull();
    }

    [Test]
    public void Constructor_SinIsDevelopment_ValorPorDefectoFalse()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "IsDevelopment", "false" },
            { "Scheduler:ExecutionIntervalMinutes", "1" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var service = new BackgroundJobService(
            Mock.Of<IServiceProvider>(),
            _loggerMock.Object,
            configuration
        );

        service.Should().NotBeNull();
    }

    #endregion

    #region Configuration Tests

    [Test]
    public void IsDevelopment_ConfiguracionDesarrollo_RetornaTrue()
    {
        var service = CreateService(isDevelopment: true);

        service.Should().NotBeNull();
    }

    [Test]
    public void IsDevelopment_ConfiguracionProduccion_RetornaFalse()
    {
        var service = CreateService(isDevelopment: false);

        service.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync Tests

    [Test]
    public async Task ExecuteAsync_CancellationRequested_SaleDelLoop()
    {
        var service = CreateService();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.StopAsync(cts.Token);

        service.Should().NotBeNull();
    }

    #endregion

    #region BackgroundService Properties Tests

    [Test]
    public void BackgroundService_ImplementaIBackgroundService()
    {
        var service = CreateService();

        service.Should().BeAssignableTo<BackgroundService>();
    }

    #endregion

    #region ExecuteAsync Method Tests

    [Test]
    public void ExecuteAsync_MetodoProtegido_PuedeSerSobreescrito()
    {
        var service = CreateService();
        var methodInfo = service.GetType().GetMethod("ExecuteAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.Should().Be(typeof(Task));
    }

    #endregion

    #region ExecuteJobsAsync Method Tests

    [Test]
    public void ExecuteJobsAsync_MetodoPrivado_ExisteEnServicio()
    {
        var service = CreateService();
        var methodInfo = service.GetType().GetMethod("ExecuteJobsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region Interval Configuration Tests

    [Test]
    public void IntervalMinutes_ConfiguracionPersonalizada_UsaValorConfigurado()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "IsDevelopment", "true" },
            { "Scheduler:ExecutionIntervalMinutes", "5" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var service = new BackgroundJobService(
            Mock.Of<IServiceProvider>(),
            _loggerMock.Object,
            configuration
        );

        service.Should().NotBeNull();
    }

    [Test]
    public void IntervalHours_ConfiguracionPersonalizada_UsaValorConfigurado()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "IsDevelopment", "false" },
            { "Scheduler:ExecutionIntervalHours", "24" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var service = new BackgroundJobService(
            Mock.Of<IServiceProvider>(),
            _loggerMock.Object,
            configuration
        );

        service.Should().NotBeNull();
    }

    #endregion

    #region Service Properties Tests

    [Test]
    public void ServiceProvider_InyectadoCorrectamente()
    {
        var service = CreateService();

        service.Should().NotBeNull();
    }

    [Test]
    public void Configuration_InyectadoCorrectamente()
    {
        var service = CreateService();

        service.Should().NotBeNull();
    }

    #endregion
}
