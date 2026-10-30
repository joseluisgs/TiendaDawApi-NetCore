using FluentAssertions;
using TiendaApi.Api.Dtos.Historial;

namespace TiendaApi.Tests.Unit.Dtos;

/// <summary>
/// Tests de los DTOs de serie y feed: records posicionales (inmutables por
/// construcción) con igualdad estructural — el mapper de Cassandra los rellena
/// campo a campo y el controller los serializa tal cual.
/// </summary>
public class HistorialDtosTests
{
    [Test]
    public void HistorialPrecioDto_EsIgualEstructural_SiMismosValores()
    {
        var fecha = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

        var a = new HistorialPrecioDto(fecha, 1299.99);
        var b = new HistorialPrecioDto(fecha, 1299.99);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Test]
    public void HistorialPrecioDto_WithNoMutaElOriginal()
    {
        var original = new HistorialPrecioDto(DateTime.UtcNow, 1299.99);

        var copia = original with { Precio = 1349.99 };

        copia.Precio.Should().Be(1349.99);
        original.Precio.Should().Be(1299.99);
    }

    [Test]
    public void VistaFeedDto_CarryaLaDenormalizacionDelNombre()
    {
        var entrada = new VistaFeedDto(DateTime.UtcNow, 1, "Laptop Dell XPS 15");

        entrada.ProductoId.Should().Be(1);
        // El nombre viene COPIADO de la fila: el feed se pinta sin cruces.
        entrada.Nombre.Should().Be("Laptop Dell XPS 15");
    }

    [Test]
    public void CambioPrecioDto_FechaEsDateOnly_DeLaParticion()
    {
        var cambio = new CambioPrecioDto(new DateOnly(2026, 10, 29), 1, 1299.99);

        cambio.Fecha.DayNumber.Should().Be(new DateOnly(2026, 10, 29).DayNumber);
        cambio.ProductoId.Should().Be(1);
    }

    [Test]
    public void LosTresDtos_SonDistintosTipos_SinRelacionDeHerencia()
    {
        // Regresion de disenno: son records independientes (no hay jerarquia).
        typeof(HistorialPrecioDto).IsAbstract.Should().BeFalse();
        typeof(VistaFeedDto).BaseType.Should().Be(typeof(object));
        typeof(CambioPrecioDto).IsSubclassOf(typeof(HistorialPrecioDto)).Should().BeFalse();
    }
}
