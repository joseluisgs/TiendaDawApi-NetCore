using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Services.Email;

/// <summary>
/// Tests unitarios para MailKitEmailService.
/// </summary>
public class EmailServiceTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<ILogger<MailKitEmailService>> _mockLogger = null!;
    private Channel<EmailMessage> _emailChannel = null!;
    private IEmailService _emailService = null!;

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

    #region Tests EnqueueEmailAsync

    /// <summary>
    /// Dado un mensaje de email válido, cuando se encola, entonces se agrega al canal.
    /// Returns: Unit.Success (mensaje encolado)
    /// </summary>
    [Test]
    public async Task EnqueueEmailAsync_MensajeValido_EncolaCorrectamente()
    {
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            IsHtml = true
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message.Should().NotBeNull();
        message!.To.Should().Be("test@example.com");
        message.Subject.Should().Be("Test Subject");
        message.Body.Should().Be("Test Body");
        message.IsHtml.Should().BeTrue();
    }

    /// <summary>
    /// Dado múltiples mensajes, cuando se encolan, entonces se mantienen en orden.
    /// Returns: Unit.Success (todos los mensajes encolados)
    /// </summary>
    [Test]
    public async Task EnqueueEmailAsync_MultiplesMensajes_MantieneOrden()
    {
        var message1 = new EmailMessage { To = "test1@example.com", Subject = "Test 1", Body = "Body 1" };
        var message2 = new EmailMessage { To = "test2@example.com", Subject = "Test 2", Body = "Body 2" };
        var message3 = new EmailMessage { To = "test3@example.com", Subject = "Test 3", Body = "Body 3" };

        await _emailService.EnqueueEmailAsync(message1);
        await _emailService.EnqueueEmailAsync(message2);
        await _emailService.EnqueueEmailAsync(message3);

        _emailChannel.Reader.TryRead(out var msg1).Should().BeTrue();
        _emailChannel.Reader.TryRead(out var msg2).Should().BeTrue();
        _emailChannel.Reader.TryRead(out var msg3).Should().BeTrue();

        msg1!.To.Should().Be("test1@example.com");
        msg2!.To.Should().Be("test2@example.com");
        msg3!.To.Should().Be("test3@example.com");
    }

    /// <summary>
    /// Dado un mensaje con body HTML, cuando se encola, entonces IsHtml es true.
    /// Returns: Unit.Success con IsHtml = true
    /// </summary>
    [Test]
    public async Task EnqueueEmailAsync_ConHtml_BodyEsHtml()
    {
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "HTML Email",
            Body = "<h1>Título</h1><p>Párrafo</p>",
            IsHtml = true
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        _emailChannel.Reader.TryRead(out var message).Should().BeTrue();
        message!.IsHtml.Should().BeTrue();
        message.Body.Should().Contain("<h1>");
    }

    /// <summary>
    /// Dado un mensaje con body texto plano, cuando se encola, entonces IsHtml es false.
    /// Returns: Unit.Success con IsHtml = false
    /// </summary>
    [Test]
    public async Task EnqueueEmailAsync_SinHtml_BodyEsTextoPlano()
    {
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Plain Text Email",
            Body = "Este es un email de texto plano",
            IsHtml = false
        };

        await _emailService.EnqueueEmailAsync(emailMessage);

        _emailChannel.Reader.TryRead(out var message).Should().BeTrue();
        message!.IsHtml.Should().BeFalse();
    }

    /// <summary>
    /// Dado un canal vacío, cuando se encola, entonces no bloquea.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task EnqueueEmailAsync_CanalVacio_EncolaSinBloqueo()
    {
        var emailMessage = new EmailMessage
        {
            To = "block@test.com",
            Subject = "Test",
            Body = "Test"
        };

        var act = async () => await _emailService.EnqueueEmailAsync(emailMessage);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests SendEmailAsync

    /// <summary>
    /// Dado SMTP no configurado (Host null), cuando se envía email, entonces se omite el envío.
    /// Returns: Unit.Success (omitido por falta de configuración)
    /// </summary>
    [Test]
    public async Task SendEmailAsync_SmtpNoConfigurado_OmiteEnvio()
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

    /// <summary>
    /// Dado SMTP configurado pero sin usuario, cuando se envía email, entonces se omite.
    /// Returns: Unit.Success (omitido por falta de credenciales)
    /// </summary>
    [Test]
    public async Task SendEmailAsync_SinUsuario_OmiteEnvio()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.example.com");
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

    /// <summary>
    /// Dado SMTP configurado con todos los valores, cuando se crea el servicio, entonces no lanza excepción.
    /// Returns: servicio creado correctamente
    /// </summary>
    [Test]
    public void SendEmailAsync_ConConfiguracionCompleta_ServicioCreado()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("smtp.gmail.com");
        _mockConfiguration.Setup(c => c["Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Smtp:Username"]).Returns("user@test.com");
        _mockConfiguration.Setup(c => c["Smtp:Password"]).Returns("password");
        _mockConfiguration.Setup(c => c["Smtp:FromEmail"]).Returns("from@test.com");
        _mockConfiguration.Setup(c => c["Smtp:FromName"]).Returns("Test Sender");

        var service = new MailKitEmailService(
            _mockConfiguration.Object,
            _mockLogger.Object,
            _emailChannel);

        service.Should().NotBeNull();
    }

    #endregion
}
