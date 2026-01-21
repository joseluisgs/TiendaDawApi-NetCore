using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Infrastructures;

/// <summary>
/// Tests unitarios para RepositoriesConfig.
/// Verifica que el repositorio de pedidos se registra correctamente según la configuración.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Infrastructure")]
public class RepositoriesConfigTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _services = new ServiceCollection();
    }

    [Test]
    public void AddRepositories_MongoDbNative_RegistraPedidosNativeRepository()
    {
        // Arrange - "Pedidos:RepositoryType" es el formato correcto
        _mockConfiguration.Setup(c => c["Pedidos:RepositoryType"]).Returns("MongoDbNative");

        // Act
        _services.AddRepositories(_mockConfiguration.Object);

        // Assert
        var descriptor = _services.Should().Contain(d => d.ServiceType == typeof(IPedidosRepository)).Subject;
        descriptor.ImplementationType.Should().Be<PedidosNativeRepository>();
    }

    [Test]
    public void AddRepositories_MongoDbEfCore_RegistraPedidosEfCoreRepository()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Pedidos:RepositoryType"]).Returns("MongoDbEfCore");

        // Act
        _services.AddRepositories(_mockConfiguration.Object);

        // Assert
        var descriptor = _services.Should().Contain(d => d.ServiceType == typeof(IPedidosRepository)).Subject;
        descriptor.ImplementationType.Should().Be<PedidosEfCoreRepository>();
    }

    [Test]
    public void AddRepositories_SinConfiguracion_RegistraPedidosNativeRepositoryPorDefecto()
    {
        // Arrange - devuelve null cuando no hay configuración
        _mockConfiguration.Setup(c => c["Pedidos:RepositoryType"]).Returns((string)null!);

        // Act
        _services.AddRepositories(_mockConfiguration.Object);

        // Assert
        var descriptor = _services.Should().Contain(d => d.ServiceType == typeof(IPedidosRepository)).Subject;
        descriptor.ImplementationType.Should().Be<PedidosNativeRepository>();
    }

    [Test]
    public void AddRepositories_Siempre_RegistraRepositoriosDeCategorias()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Pedidos:RepositoryType"]).Returns("MongoDbNative");

        // Act
        _services.AddRepositories(_mockConfiguration.Object);

        // Assert
        _services.Should().Contain(d => d.ServiceType == typeof(TiendaApi.Api.Repositories.Categorias.ICategoriaRepository));
    }
}
