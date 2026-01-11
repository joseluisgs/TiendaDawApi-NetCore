using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Services.Email;

namespace TiendaApi.Tests;

/// <summary>
/// Smoke tests for email service
/// </summary>
public class EmailServiceTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Channel<EmailMessage> _emailChannel = null!;
    private IEmailService _emailService = null!;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _emailChannel = Channel.CreateUnbounded<EmailMessage>();
        var mockLogger = new Mock<ILogger<MailKitEmailService>>();
        
        _emailService = new MailKitEmailService(
            _mockConfiguration.Object,
            mockLogger.Object,
            _emailChannel);
    }

    [Test]
    public async Task EnqueueEmailAsync_ShouldAddMessageToChannel()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            IsHtml = true
        };

        // Act
        await _emailService.EnqueueEmailAsync(emailMessage);

        // Assert
        var canRead = _emailChannel.Reader.TryRead(out var message);
        canRead.Should().BeTrue();
        message.Should().NotBeNull();
        message!.To.Should().Be("test@example.com");
        message.Subject.Should().Be("Test Subject");
    }

    [Test]
    public async Task EnqueueEmailAsync_MultipleMessages_ShouldQueueAll()
    {
        // Arrange
        var message1 = new EmailMessage { To = "test1@example.com", Subject = "Test 1", Body = "Body 1" };
        var message2 = new EmailMessage { To = "test2@example.com", Subject = "Test 2", Body = "Body 2" };

        // Act
        await _emailService.EnqueueEmailAsync(message1);
        await _emailService.EnqueueEmailAsync(message2);

        // Assert
        _emailChannel.Reader.TryRead(out var msg1).Should().BeTrue();
        _emailChannel.Reader.TryRead(out var msg2).Should().BeTrue();
        msg1!.To.Should().Be("test1@example.com");
        msg2!.To.Should().Be("test2@example.com");
    }
}
