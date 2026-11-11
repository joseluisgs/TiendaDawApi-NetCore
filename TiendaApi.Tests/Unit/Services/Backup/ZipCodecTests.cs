using System.IO.Compression;
using System.Text;
using FluentAssertions;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Backup;
using TiendaApi.Api.Services.Backup;

namespace TiendaApi.Tests.Unit.Services.Backup;

/// <summary>
///     Tests unitarios de ZipCodec: round-trip de entradas y, sobre todo, las
///     protecciones contra ficheros hostiles (demasiadas entradas o descomprimido
///     demasiado grande).
/// </summary>
[TestFixture]
public class ZipCodecTests {
    private static MemoryStream CrearZip(params (string Nombre, string Contenido)[] entradas) {
        var memoria = new MemoryStream();
        using (ZipArchive archivo = ZipCodec.Crear(memoria)) {
            foreach ((string nombre, string contenido) in entradas) {
                ZipArchiveEntry entrada = archivo.CreateEntry(nombre);
                using Stream flujo = entrada.Open();
                using var escritor = new StreamWriter(flujo, new UTF8Encoding(false));
                escritor.Write(contenido);
            }
        }

        memoria.Position = 0;
        return memoria;
    }

    [Test]
    public void RoundTrip_LasEntradasSeLeenIgualesQueSeEscribieron() {
        using MemoryStream zip = CrearZip(
            ("manifest.json", """{"version":"1.0"}"""),
            ("productos.json", """[{"id":1,"nombre":"Laptop"}]"""));

        IReadOnlyList<ZipCodec.EntradaZip> entradas = ZipCodec.Leer(zip);

        entradas.Should().HaveCount(2);
        entradas[0].Nombre.Should().Be("manifest.json");
        entradas[0].Contenido.Should().Contain("\"version\"");
        entradas[1].Contenido.Should().Contain("Laptop");
    }

    [Test]
    public void Lectura_UnZipVacio_NoDevuelveNada() {
        using MemoryStream zip = CrearZip();

        ZipCodec.Leer(zip).Should().BeEmpty();
    }

    [Test]
    public void Lectura_ConDemasiadasEntradas_SeRechaza() {
        (string, string)[] entradas = Enumerable.Range(0, ZipCodec.MaxEntradas + 1)
            .Select(i => ($"fichero-{i}.json", "[]"))
            .ToArray();
        using MemoryStream zip = CrearZip(entradas);

        Func<IReadOnlyList<ZipCodec.EntradaZip>> accion = () => ZipCodec.Leer(zip);

        accion.Should().Throw<BackupException>()
            .Which.Error.Should().BeOfType<ValidationError>(
                "el límite de entradas es un error de formato, no un fallo interno");
    }

    [Test]
    public void Lectura_ConMasDeCincuentaMb_SeRechaza() {
        // Un zip bomb: pocos bytes comprimidos que ocupan mucho al descomprimir.
        string grande = new('a', ZipCodec.MaxBytesDescomprimidos + 1024);
        using MemoryStream zip = CrearZip(("grande.json", grande));

        Func<IReadOnlyList<ZipCodec.EntradaZip>> accion = () => ZipCodec.Leer(zip);

        accion.Should().Throw<BackupException>()
            .Which.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public void Entradas_DevuelveLosNombresCompletos() {
        using MemoryStream zip = CrearZip(("subcarpeta/datos.json", "[]"));

        IReadOnlyList<ZipCodec.EntradaZip> entradas = ZipCodec.Leer(zip);

        entradas.Should().ContainSingle().Which.Nombre.Should().Be("subcarpeta/datos.json");
    }
}
