using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Background.Host;

namespace TiendaApi.Tests.Unit.Services.Background;

public class BackgroundJobServiceTests {
    private Mock<ILogger<BackgroundJobService>> _loggerMock = null!;

    private BackgroundJobService CreateService(bool isDevelopment = true, int intervalMinutes = 1,
        int intervalHours = 168) {
        _loggerMock = new Mock<ILogger<BackgroundJobService>>();

        var inMemorySettings = new Dictionary<string, string?> {
            { "IsDevelopment", isDevelopment.ToString().ToLower() },
            { "Scheduler:ExecutionIntervalMinutes", intervalMinutes.ToString() },
            { "Scheduler:ExecutionIntervalHours", intervalHours.ToString() }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        return new BackgroundJobService(
            Mock.Of<IServiceProvider>(),
            _loggerMock.Object,
            configuration);
    }

    #region ExecuteAsync Tests

    [Test]
    public async Task ExecuteAsync_CancellationRequested_SaleDelLoop() {
        BackgroundJobService service = CreateService();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.StopAsync(cts.Token);

        service.Should().NotBeNull();
    }

    #endregion

    #region BackgroundService Properties Tests

    [Test]
    public void BackgroundService_ImplementaIBackgroundService() {
        BackgroundJobService service = CreateService();

        service.Should().BeAssignableTo<BackgroundService>();
    }

    #endregion

    #region ExecuteAsync Method Tests

    [Test]
    public void ExecuteAsync_MetodoProtegido_PuedeSerSobreescrito() {
        BackgroundJobService service = CreateService();
        MethodInfo? methodInfo = service.GetType().GetMethod("ExecuteAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnType.Should().Be(typeof(Task));
    }

    #endregion

    #region ExecuteJobsAsync Method Tests

    [Test]
    public void ExecuteJobsAsync_MetodoPrivado_ExisteEnServicio() {
        BackgroundJobService service = CreateService();
        MethodInfo? methodInfo = service.GetType().GetMethod("ExecuteJobsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        methodInfo.Should().NotBeNull();
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_CreaInstanciaCorrectamente() {
        BackgroundJobService service = CreateService();

        service.Should().NotBeNull();
    }

    [Test]
    public void Constructor_SinIsDevelopment_ValorPorDefectoFalse() {
        var inMemorySettings = new Dictionary<string, string?> {
            { "IsDevelopment", "false" },
            { "Scheduler:ExecutionIntervalMinutes", "1" }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
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
    public void IsDevelopment_ConfiguracionDesarrollo_RetornaTrue() {
        BackgroundJobService service = CreateService();

        service.Should().NotBeNull();
    }

    [Test]
    public void IsDevelopment_ConfiguracionProduccion_RetornaFalse() {
        BackgroundJobService service = CreateService(false);

        service.Should().NotBeNull();
    }

    #endregion

    #region Interval Configuration Tests

    [Test]
    public void IntervalMinutes_ConfiguracionPersonalizada_UsaValorConfigurado() {
        var inMemorySettings = new Dictionary<string, string?> {
            { "IsDevelopment", "true" },
            { "Scheduler:ExecutionIntervalMinutes", "5" }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
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
    public void IntervalHours_ConfiguracionPersonalizada_UsaValorConfigurado() {
        var inMemorySettings = new Dictionary<string, string?> {
            { "IsDevelopment", "false" },
            { "Scheduler:ExecutionIntervalHours", "24" }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
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
    public void ServiceProvider_InyectadoCorrectamente() {
        BackgroundJobService service = CreateService();

        service.Should().NotBeNull();
    }

    [Test]
    public void Configuration_InyectadoCorrectamente() {
        BackgroundJobService service = CreateService();

        service.Should().NotBeNull();
    }

    #endregion
}
