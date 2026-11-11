using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Background.Jobs;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Services.Background;

/// <summary>
///     Tests unitarios para ProductoReportTask.
///     Verifica la lógica de reportes de productos y envío de emails.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("BackgroundJob")]
public class ProductoReportTaskTests {
    [SetUp]
    public void SetUp() {
        _mockProductoRepository = new Mock<IProductoRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<ProductoReportTask>>();
    }

    private Mock<IProductoRepository> _mockProductoRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<ILogger<ProductoReportTask>> _mockLogger = null!;

    private ProductoReportTask CreateTask(bool isDevelopment = true, int days = 7) {
        var inMemorySettings = new Dictionary<string, string?> {
            { "Scheduler:ProductoReportDays", days.ToString() },
            { "IsDevelopment", isDevelopment.ToString().ToLower() }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        return new ProductoReportTask(
            _mockProductoRepository.Object,
            _mockUserRepository.Object,
            _mockEmailService.Object,
            _mockLogger.Object,
            configuration
        );
    }

    [Test]
    public async Task ExecuteAsync_ModoDesarrollo_RetornaSuccess() {
        // Arrange
        ProductoReportTask task = CreateTask();

        // Act
        UnitResult<DomainError> result = await task.ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_ModoProduccion_SinProductos_RetornaSuccess() {
        // Arrange
        ProductoReportTask task = CreateTask(false);

        _mockProductoRepository.Setup(r => r.GetRecentlyCreatedAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Producto>());

        // Act
        UnitResult<DomainError> result = await task.ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_ModoProduccion_ConProductos_EnviaEmails() {
        // Arrange
        ProductoReportTask task = CreateTask(false);

        var productos = new List<Producto> {
            new() { Id = 1, Nombre = "Producto 1", Descripcion = "Desc 1", Precio = 10.99m, Stock = 5 },
            new() { Id = 2, Nombre = "Producto 2", Descripcion = "Desc 2", Precio = 20.99m, Stock = 10 }
        };

        var usuarios = new List<User> {
            new() { Id = 1, UserName = "user1", Email = "user1@example.com" },
            new() { Id = 2, UserName = "user2", Email = "user2@example.com" }
        };

        _mockProductoRepository.Setup(r => r.GetRecentlyCreatedAsync(7))
            .ReturnsAsync(productos);

        _mockUserRepository.Setup(r => r.GetActiveUsersAsync())
            .ReturnsAsync(usuarios);

        _mockEmailService.Setup(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        UnitResult<DomainError> result = await task.ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockEmailService.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Exactly(2));
    }

    [Test]
    public async Task ExecuteAsync_ModoProduccion_SinUsuarios_RetornaSuccess() {
        // Arrange
        ProductoReportTask task = CreateTask(false);

        var productos = new List<Producto> {
            new() { Id = 1, Nombre = "Producto 1", Descripcion = "Desc 1", Precio = 10.99m, Stock = 5 }
        };

        _mockProductoRepository.Setup(r => r.GetRecentlyCreatedAsync(7))
            .ReturnsAsync(productos);

        _mockUserRepository.Setup(r => r.GetActiveUsersAsync())
            .ReturnsAsync(new List<User>());

        // Act
        UnitResult<DomainError> result = await task.ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockEmailService.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_ModoProduccion_DiasPersonalizados_UsaDiasConfigurados() {
        // Arrange
        ProductoReportTask task = CreateTask(false, 14);

        var productos = new List<Producto>();

        _mockProductoRepository.Setup(r => r.GetRecentlyCreatedAsync(14))
            .ReturnsAsync(productos);

        // Act
        UnitResult<DomainError> result = await task.ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockProductoRepository.Verify(r => r.GetRecentlyCreatedAsync(14), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_ModoProduccion_UnProducto_EnviaUnEmail() {
        // Arrange
        ProductoReportTask task = CreateTask(false);

        var productos = new List<Producto> {
            new() { Id = 1, Nombre = "Producto 1", Descripcion = "Desc 1", Precio = 10.99m, Stock = 5 }
        };

        var usuarios = new List<User> {
            new() { Id = 1, UserName = "user1", Email = "user1@example.com" }
        };

        _mockProductoRepository.Setup(r => r.GetRecentlyCreatedAsync(7))
            .ReturnsAsync(productos);

        _mockUserRepository.Setup(r => r.GetActiveUsersAsync())
            .ReturnsAsync(usuarios);

        _mockEmailService.Setup(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        UnitResult<DomainError> result = await task.ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockEmailService.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Once);
    }

    [Test]
    public void Constructor_ValoresPorDefecto_ConfiguraCorrectamente() {
        // Arrange & Act
        ProductoReportTask task = CreateTask();

        // Assert - The task was created successfully
        task.Should().NotBeNull();
    }
}
