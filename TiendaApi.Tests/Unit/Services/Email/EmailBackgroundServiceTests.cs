using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Services.Email;

/// <summary>
/// Tests unitarios para EmailBackgroundService.
/// </summary>
public class EmailBackgroundServiceTests
{
    private Channel<EmailMessage> _emailChannel = null!;

    [SetUp]
    public void Setup()
    {
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
    }

    [Test]
    public void Constructor_ParametrosValidos_NoLanzaExcepcion()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<EmailBackgroundService>>();

        var act = () => new EmailBackgroundService(
            _emailChannel,
            mockServiceProvider.Object,
            mockLogger.Object);

        act.Should().NotThrow();
    }
}
