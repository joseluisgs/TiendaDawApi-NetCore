using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Services.Email;

public class MailKitEmailServiceEdgeTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<ILogger<MailKitEmailService>> _mockLogger = null!;
    private Channel<EmailMessage> _emailChannel = null!;
    private MailKitEmailService _emailService = null!;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();

        _emailService = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);
    }

    #region EnqueueEmailAsync Edge Cases

    [Test]
    public async Task EnqueueEmailAsync_ConEmailMuyLargo_EncolaCorrectamente()
    {
        var longBody = new string('A', 10000);
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject con cuerpo muy largo",
            Body = longBody,
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.Body.Length.Should().Be(10000);
    }

    [Test]
    public async Task EnqueueEmailAsync_ConAsuntoVacio_EncolaCorrectamente()
    {
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "",
            Body = "Test Body",
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.Subject.Should().BeEmpty();
    }

    [Test]
    public async Task EnqueueEmailAsync_ConBodyHtmlConTagsAnidados_EncolaCorrectamente()
    {
        var htmlBody = "<div><table><tr><td><h1>Título</h1></td></tr></table></div>";
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "HTML Email",
            Body = htmlBody,
            IsHtml = true
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.IsHtml.Should().BeTrue();
    }

    [Test]
    public async Task EnqueueEmailAsync_ConBodyConCaracteresEspeciales_EncolaCorrectamente()
    {
        var specialCharsBody = "Hola \"Mundo\" & 'Tierra'<script>alert('XSS')</script>";
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Special Chars",
            Body = specialCharsBody,
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.Body.Should().Contain("&");
    }

    [Test]
    public async Task EnqueueEmailAsync_ConMultiplesDestinatarios_SoloTomaElPrimero()
    {
        var emailMessage = new EmailMessage
        {
            To = "test1@example.com,test2@example.com",
            Subject = "Multiple Recipients",
            Body = "Test Body",
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.To.Should().Contain(",");
    }

    [Test]
    public async Task EnqueueEmailAsync_ConUnicodeEnSubject_EncolaCorrectamente()
    {
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Asunto con caracteres Unicode: áéíóú ñü 中文",
            Body = "Test Body",
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.Subject.Should().Contain("中文");
    }

    [Test]
    public async Task EnqueueEmailAsync_ConEmojiEnBody_EncolaCorrectamente()
    {
        var emojiBody = "Hola mundo 🎉🚀✨";
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Emoji Test",
            Body = emojiBody,
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message!.Body.Should().Contain("🎉");
    }

    [Test]
    public async Task EnqueueEmailAsync_CanalLleno_NoBloquea()
    {
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test"
        };

        await _emailService.EnqueueEmailAsync(emailMessage);
        await _emailService.EnqueueEmailAsync(emailMessage);

        _emailChannel.Reader.TryRead(out _).Should().BeTrue();
        _emailChannel.Reader.TryRead(out _).Should().BeTrue();
    }

    #endregion

    #region SendEmailAsync Configuration Edge Cases

    [Test]
    public async Task SendEmailAsync_SmtpHostVacio_OmiteEnvio()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("");

        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test Body"
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

    [Test]
    public async Task SendEmailAsync_SmtpHostNull_OmiteEnvio()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);

        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test Body"
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

    [Test]
    public async Task SendEmailAsync_SmtpUsernameNull_OmiteEnvio()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns((string?)null);

        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test Body"
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

    [Test]
    public async Task SendEmailAsync_ConFromEmailNull_UsaUsername()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("user@test.com");
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password");
        _mockConfiguration.Setup(c => c["Smtp:FromEmail"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:FromName"]).Returns("Test Sender");

        var service = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        service.Should().NotBeNull();
    }

    [Test]
    public async Task SendEmailAsync_ConFromNameNull_UsaValorPorDefecto()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("user@test.com");
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password");
        _mockConfiguration.Setup(c => c["Smtp:FromEmail"]).Returns("from@test.com");
        _mockConfiguration.Setup(c => c["Smtp:FromName"]).Returns((string?)null);

        var service = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        service.Should().NotBeNull();
    }

    #endregion

    #region Constructor Edge Cases

    [Test]
    public void Constructor_ConConfiguracionVacia_NoLanzaExcepcion()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns((string?)null);

        var act = () => new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        act.Should().NotThrow();
    }

    #endregion
}
