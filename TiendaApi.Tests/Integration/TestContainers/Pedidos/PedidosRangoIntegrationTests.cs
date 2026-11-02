using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NUnit.Framework;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos;

/// <summary>
/// Tests de integración del repositorio nativo de pedidos sobre MongoDB.
///
/// Se centran en <c>FindByRangoFechasAsync</c>, el método nuevo de la ventana
/// estadística: el endpoint E2E solo puede probar un rango amplio (todo entra),
/// así que aquí se comprueba lo interesante — que los pedidos FUERA de la ventana
/// no se devuelvan y que el orden sea ascendente por fecha.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class PedidosRangoIntegrationTests
{
    private const string DatabaseName = "it_pedidos_rango";

    private string _mongoConnectionString = string.Empty;
    private IServiceProvider? _provider;
    private IPedidosRepository? _repositorio;
    private IMongoCollection<Pedido>? _coleccion;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _mongoConnectionString = AssemblyContainerFixture.MongoConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "MongoDbSettings:ConnectionString", _mongoConnectionString },
                { "MongoDbSettings:DatabaseName", DatabaseName }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var cliente = new MongoClient(_mongoConnectionString);
        services.AddSingleton(cliente);
        services.AddSingleton(cliente.GetDatabase(DatabaseName));
        services.AddScoped<ILogger<PedidosNativeRepository>, Logger<PedidosNativeRepository>>();
        services.AddScoped<IPedidosRepository, PedidosNativeRepository>();

        _provider = services.BuildServiceProvider();
        _repositorio = _provider.GetRequiredService<IPedidosRepository>();
        _coleccion = cliente.GetDatabase(DatabaseName).GetCollection<Pedido>("pedidos");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_provider is IDisposable desechable) desechable.Dispose();
        AssemblyContainerFixture.DropMongoDatabase(DatabaseName);
    }

    [SetUp]
    public async Task SetUp()
    {
        // Cada test parte de una colección limpia para no depender del orden.
        await _coleccion!.DeleteManyAsync(FilterDefinition<Pedido>.Empty);
    }

    private static Pedido Pedido(long id, DateTime creado, decimal total = 100m) =>
        new()
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId(),
            UserId = id,
            Total = total,
            Estado = PedidoEstado.ENTREGADO,
            CreatedAt = creado,
            IsDeleted = false,
            Items = [],
        };

    [Test]
    public async Task FindByRangoFechas_RetornaSoloLosPedidosDeLaVentana()
    {
        var dentro1 = Pedido(1, new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc));
        var dentro2 = Pedido(2, new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc));
        var fueraAntes = Pedido(3, new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
        var fueraDespues = Pedido(4, new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc));
        await _coleccion!.InsertManyAsync([dentro1, dentro2, fueraAntes, fueraDespues]);

        var resultado = await _repositorio!.FindByRangoFechasAsync(
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));

        resultado.Should().HaveCount(2, "los pedidos de septiembre y noviembre quedan fuera");
        resultado.Select(p => p.UserId).Should().BeEquivalentTo(new[] { 1L, 2L });
    }

    [Test]
    public async Task FindByRangoFechas_DevuelveOrdenadoPorFechaAscendente()
    {
        await _coleccion!.InsertManyAsync(
        [
            Pedido(1, new DateTime(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc)),
            Pedido(2, new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc)),
            Pedido(3, new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc)),
        ]);

        var resultado = (await _repositorio!.FindByRangoFechasAsync(
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc))).ToList();

        resultado.Should().HaveCount(3);
        resultado.Select(p => p.UserId)
            .Should().Equal(new long[] { 2, 3, 1 }, "ascendente: el 5 antes que el 15 y el 25");
    }

    [Test]
    public async Task FindByRangoFechas_LaVentanaEsExclusivaAlFinal()
    {
        // El límite superior es exclusivo: un pedido creado exactamente en `hasta`
        // pertenece a la ventana siguiente.
        var enElLimite = Pedido(1, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
        var justoAntes = Pedido(2, new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc));
        await _coleccion!.InsertManyAsync([enElLimite, justoAntes]);

        var resultado = (await _repositorio!.FindByRangoFechasAsync(
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc))).ToList();

        resultado.Should().ContainSingle().Which.UserId.Should().Be(2);
    }
}
