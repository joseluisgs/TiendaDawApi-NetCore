using System.Text;
using FluentAssertions;
using TiendaApi.Api.Services.Backup;

namespace TiendaApi.Tests.Unit.Services.Backup;

/// <summary>
///     Tests unitarios de CsvCodec: la escritura y la lectura deben ser inversas
///     (round-trip), que es donde se rompe un CSV casero.
/// </summary>
[TestFixture]
public class CsvCodecTests {
    private static string Escribir(params string[][] filas) {
        using var texto = new StringWriter();
        foreach (string[] fila in filas) {
            CsvCodec.EscribirFila(texto, fila);
        }

        return texto.ToString();
    }

    private static List<string[]> Leer(string contenido) {
        using var lector = new StringReader(contenido);
        return CsvCodec.Leer(lector);
    }

    [Test]
    public void Escritor_CamposSinCaracteresEspeciales_NoEntrecomilla() {
        string csv = Escribir(["hola", "mundo"]);

        csv.Should().Be("hola,mundo" + Environment.NewLine);
    }

    [Test]
    public void Escritor_CamposConComaComillaOSalto_LosEntrecomilla() {
        string csv = Escribir(["Laptop, tope de gama", "Di \"s\"count", "linea1\nlinea2"]);

        csv.Should().StartWith("\"Laptop, tope de gama\",\"Di \"\"s\"\"count\"");
        csv.Should().Contain("\"linea1\nlinea2\"");
    }

    [Test]
    public void RoundTrip_ConComillasComasYSaltos_SeRecuperanIguales() {
        string[][] original = new[] {
            new[] { "nombre", "descripcion" },
            new[] { "Camiseta, negra", "Talla \"M\"" },
            new[] { "Saltos\nen línea", "con, coma" },
            new[] { "normal", "" }
        };

        string csv = Escribir(original);
        List<string[]> leidas = Leer(csv);

        leidas.Should().HaveCount(original.Length);
        leidas[1][0].Should().Be("Camiseta, negra");
        leidas[1][1].Should().Be("Talla \"M\"");
        leidas[2][0].Should().Be("Saltos\nen línea");
        leidas[2][1].Should().Be("con, coma");
        leidas[3][1].Should().BeEmpty();
    }

    [Test]
    public void RoundTrip_ConSaltoDeLineaCRLF_SeRecuperanLasFilas() {
        string csv = "a,b\r\n1,2\r\n3,4\r\n";

        List<string[]> filas = Leer(csv);

        filas.Should().HaveCount(3);
        filas[1].Should().Equal("1", "2");
        filas[2].Should().Equal("3", "4");
    }

    [Test]
    public void Lectura_SinSaltoFinal_NoPierdeLaUltimaFila() {
        List<string[]> filas = Leer("a,b\n1,2");

        filas.Should().HaveCount(2, "la última fila no termina en salto de línea");
        filas[1].Should().Equal("1", "2");
    }

    [Test]
    public void Lectura_LineaVacia_NoProduceUnaFilaFantasma() {
        List<string[]> filas = Leer("a,b\n\n1,2\n");

        filas.Should().HaveCount(2);
        filas[1].Should().Equal("1", "2");
    }

    [Test]
    public void Escritor_EmiteBomDeUtf8ParaExcel() {
        using var memoria = new MemoryStream();
        using (StreamWriter escritor = CsvCodec.CrearEscritor(memoria)) {
            CsvCodec.EscribirCabecera(escritor, ["nombre"]);
            CsvCodec.EscribirFila(escritor, ["Electrónica"]);
        }

        byte[] bytes = memoria.ToArray();
        bytes[0].Should().Be(0xEF, "BOM UTF-8 para que Excel abra los acentos");
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);
        Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).Should().Contain("Electrónica");
    }

    [Test]
    public void Formatear_NumerosYFechas_UsanCulturaInvariante() {
        CsvCodec.Formatear(1234.56m).Should().Be("1234.56", "punto decimal, sin depender del idioma");
        CsvCodec.Formatear(new DateTime(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc))
            .Should().Be("2026-11-02T10:30:00Z");
    }

    [Test]
    public void Parseadores_ConValoresRaros_DevuelvenNull() {
        CsvCodec.ADecimal("").Should().BeNull();
        CsvCodec.AEnteroLargo("no-numero").Should().BeNull();
        CsvCodec.AFecha("").Should().BeNull();
        CsvCodec.ABooleano("true").Should().BeTrue();
        CsvCodec.ABooleano("1").Should().BeTrue();
        CsvCodec.ABooleano("false").Should().BeFalse();
    }
}
