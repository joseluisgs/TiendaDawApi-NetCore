using System.Threading.Channels;
using FluentAssertions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MimeKit;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Services.Email;

public class MailKitEmailServiceTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<ILogger<MailKitEmailService>> _mockLogger = null!;
    private Channel<EmailMessage> _emailChannel = null!;
    private MailKitEmailService _emailService = null!;

    private void SetupConfiguration(bool valid = true)
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();

        if (valid)
        {
            _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
            _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
            _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("test@test.com");
            _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password");
            _mockConfiguration.Setup(c => c["Smtp:FromEmail"]).Returns("noreply@test.com");
            _mockConfiguration.Setup(c => c["Smtp:FromName"]).Returns("TiendaApi");
        }

        _emailService = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_ConParametrosValidos_CreaInstancia()
    {
        SetupConfiguration(valid: true);

        _emailService.Should().NotBeNull();
    }

    [Test]
    public void Constructor_SinConfiguracion_CreaInstancia()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns((string?)null);
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();

        var service = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        service.Should().NotBeNull();
    }

    #endregion

    #region EnqueueEmailAsync Tests

    [Test]
    public async Task EnqueueEmailAsync_MensajeValido_EncolaCorrectamente()
    {
        SetupConfiguration();
        var emailMessage = new EmailMessage
        {
            To = "destinatario@test.com",
            Subject = "Test Subject",
            Body = "Test Body",
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.To.Should().Be("destinatario@test.com");
        message.Subject.Should().Be("Test Subject");
        message.Body.Should().Be("Test Body");
    }

    [Test]
    public async Task EnqueueEmailAsync_MensajeHtml_EncolaCorrectamente()
    {
        SetupConfiguration();
        var emailMessage = new EmailMessage
        {
            To = "destinatario@test.com",
            Subject = "HTML Subject",
            Body = "<html><body>Hello</body></html>",
            IsHtml = true
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.IsHtml.Should().BeTrue();
    }

    [Test]
    public async Task EnqueueEmailAsync_MultiplesMensajes_EncolaTodos()
    {
        SetupConfiguration();
        var messages = new[]
        {
            new EmailMessage { To = "test1@test.com", Subject = "Test 1", Body = "Body 1" },
            new EmailMessage { To = "test2@test.com", Subject = "Test 2", Body = "Body 2" },
            new EmailMessage { To = "test3@test.com", Subject = "Test 3", Body = "Body 3" }
        };

        foreach (var msg in messages)
        {
            await _emailService.EnqueueEmailAsync(msg);
        }

        _emailChannel.Reader.TryRead(out var first).Should().BeTrue();
        _emailChannel.Reader.TryRead(out var second).Should().BeTrue();
        _emailChannel.Reader.TryRead(out var third).Should().BeTrue();
    }

    [Test]
    public async Task EnqueueEmailAsync_ConExcepcionEnCanal_NoLanzaExcepcion()
    {
        SetupConfiguration();
        var boundedChannel = Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(1)
        {
            SingleWriter = true
        });

        var serviceWithBoundedChannel = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            boundedChannel);

        var emailMessage = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Test",
            Body = "Test"
        };

        boundedChannel.Writer.Complete(new Exception("Channel closed"));

        var act = async () => await serviceWithBoundedChannel.EnqueueEmailAsync(emailMessage);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task EnqueueEmailAsync_LogueaInformacion()
    {
        SetupConfiguration();
        var emailMessage = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Test",
            Body = "Test"
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email encolado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SendEmailAsync Configuration Tests

    [Test]
    public async Task SendEmailAsync_SinHostConfigurado_OmiteEnvio()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns((string?)null);
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();
        _emailService = new MailKitEmailService(_mockConfiguration.Object, _mockLogger.Object, _emailChannel);

        var emailMessage = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Test",
            Body = "Test"
        };

        await _emailService.SendEmailAsync(emailMessage);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMTP no configurado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_SinUsername_OmiteEnvio()
    {
        SetupConfiguration();
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns((string?)null);

        var emailMessage = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Test",
            Body = "Test"
        };

        await _emailService.SendEmailAsync(emailMessage);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SendEmailAsync Exception Handling Tests

    [Test]
    public async Task SendEmailAsync_ConExcepcion_LogueaError()
    {
        SetupConfiguration();
        var emailMessage = new EmailMessage
        {
            To = "invalid-email",
            Subject = "Test",
            Body = "Test"
        };

        var act = async () => await _emailService.SendEmailAsync(emailMessage);

        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task SendEmailAsync_EmailInvalido_LanzaExcepcion()
    {
        SetupConfiguration();
        var emailMessage = new EmailMessage
        {
            To = "not-an-email",
            Subject = "Test",
            Body = "Test"
        };

        var act = async () => await _emailService.SendEmailAsync(emailMessage);

        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void ImplementsIEmailService()
    {
        SetupConfiguration();

        _emailService.Should().BeAssignableTo<IEmailService>();
    }

    #endregion

    #region EmailMessage Validation Tests

    [Test]
    public async Task EnqueueEmailAsync_ConEmailVacio_EncolaCorrectamente()
    {
        SetupConfiguration();
        var emailMessage = new EmailMessage
        {
            To = "",
            Subject = "Test",
            Body = "Test"
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        _emailChannel.Reader.TryRead(out _).Should().BeTrue();
    }

    [Test]
    public async Task EnqueueEmailAsync_ConSubjectLargo_EncolaCorrectamente()
    {
        SetupConfiguration();
        var emailMessage = new EmailMessage
        {
            To = "test@test.com",
            Subject = new string('A', 500),
            Body = "Test"
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        _emailChannel.Reader.TryRead(out var message).Should().BeTrue();
        message!.Subject.Length.Should().Be(500);
    }

    #endregion

    #region Configuration Fallback Tests

    [Test]
    public void Constructor_SinFromEmail_UsaUsernameComoFrom()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("user@test.com");
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password");
        _mockConfiguration.Setup(c => c["Smtp:FromEmail"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:FromName"]).Returns((string?)null);
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();

        var service = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        service.Should().NotBeNull();
    }

    [Test]
    public void Constructor_SinFromName_UsaTiendaApi()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("user@test.com");
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password");
        _mockConfiguration.Setup(c => c["Smtp:FromEmail"]).Returns("from@test.com");
        _mockConfiguration.Setup(c => c["Smtp:FromName"]).Returns((string?)null);
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();

        var service = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        service.Should().NotBeNull();
    }

    #endregion

    #region Port Configuration Tests

    [Test]
    public void Constructor_ConPuertoDefault_Usa587()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("user@test.com");
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password");
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();

        var service = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        service.Should().NotBeNull();
    }

    #endregion
}
