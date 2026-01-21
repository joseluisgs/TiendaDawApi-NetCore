using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Services.Email;

/// <summary>
/// Tests unitarios para MemoryEmailService.
/// </summary>
public class MemoryEmailServiceTests
{
    private Mock<ILogger<MemoryEmailService>> _mockLogger = null!;
    private MemoryEmailService _emailService = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MemoryEmailService>>();
        _emailService = new MemoryEmailService(_mockLogger.Object);
    }

    #region EnqueueEmailAsync Tests

    [Test]
    public async Task EnqueueEmailAsync_EmailValido_LogueaContenido()
    {
        var email = CreateTestEmail();

        var act = async () => await _emailService.EnqueueEmailAsync(email);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ENQUEUED")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    [Test]
    public async Task EnqueueEmailAsync_EmailHtml_LogueaTipoHtml()
    {
        var email = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "<html><body>Test</body></html>",
            IsHtml = true
        };

        var act = async () => await _emailService.EnqueueEmailAsync(email);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTML")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    [Test]
    public async Task EnqueueEmailAsync_EmailTexto_LogueaTipoTexto()
    {
        var email = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Plain text body",
            IsHtml = false
        };

        var act = async () => await _emailService.EnqueueEmailAsync(email);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Texto")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    [Test]
    public async Task EnqueueEmailAsync_DestinatarioCorrecto_LogueaTo()
    {
        var email = new EmailMessage
        {
            To = "admin@tienda.com",
            Subject = "Test",
            Body = "Body"
        };

        var act = async () => await _emailService.EnqueueEmailAsync(email);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("admin@tienda.com")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    #endregion

    #region SendEmailAsync Tests

    [Test]
    public async Task SendEmailAsync_EmailValido_LogueaContenido()
    {
        var email = CreateTestEmail();

        var act = async () => await _emailService.SendEmailAsync(email);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SENT")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_AsuntoCorrecto_LogueaAsunto()
    {
        var email = new EmailMessage
        {
            To = "test@example.com",
            Subject = "🛒 Nuevo Pedido Creado",
            Body = "Body"
        };

        var act = async () => await _emailService.SendEmailAsync(email);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("🛒 Nuevo Pedido Creado")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_CuerpoHtml_LogueaDebug()
    {
        var email = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "<html><body>Full HTML content</body></html>",
            IsHtml = true
        };

        var act = async () => await _emailService.SendEmailAsync(email);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cuerpo")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    #endregion

    private static EmailMessage CreateTestEmail()
    {
        return new EmailMessage
        {
            To = "admin@tienda.com",
            Subject = "🛒 Nuevo Pedido #123",
            Body = "<html><body>Pedido creado exitosamente</body></html>",
            IsHtml = true
        };
    }
}
