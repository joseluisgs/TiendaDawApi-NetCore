using System.Text;
using FluentAssertions;
using NUnit.Framework;
using TiendaApi.Api.Errors.Backup;
using TiendaApi.Api.Services.Backup;

namespace TiendaApi.Tests.Unit.Services.Backup;

/// <summary>
/// Tests unitarios de ZipCodec: round-trip de entradas y, sobre todo, las
/// protecciones contra ficheros hostiles (demasiadas entradas o descomprimido
/// demasiado grande).
/// </summary>
[TestFixture]
public class ZipCodecTests
{
    private static MemoryStream CrearZip(params (string Nombre, string Contenido)[] entradas)
    {
        var memoria = new MemoryStream();
        using (var archivo = ZipCodec.Crear(memoria))
        {
            foreach (var (nombre, contenido) in entradas)
            {
                var entrada = archivo.CreateEntry(nombre);
                using var flujo = entrada.Open();
                using var escritor = new StreamWriter(flujo, new UTF8Encoding(false));
                escritor.Write(contenido);
            }
        }

        memoria.Position = 0;
        return memoria;
    }

    [Test]
    public void RoundTrip_LasEntradasSeLeenIgualesQueSeEscribieron()
    {
        using var zip = CrearZip(
            ("manifest.json", """{"version":"1.0"}"""),
            ("productos.json", """[{"id":1,"nombre":"Laptop"}]"""));

        var entradas = ZipCodec.Leer(zip);

        entradas.Should().HaveCount(2);
        entradas[0].Nombre.Should().Be("manifest.json");
        entradas[0].Contenido.Should().Contain("\"version\"");
        entradas[1].Contenido.Should().Contain("Laptop");
    }

    [Test]
    public void Lectura_UnZipVacio_NoDevuelveNada()
    {
        using var zip = CrearZip();

        ZipCodec.Leer(zip).Should().BeEmpty();
    }

    [Test]
    public void Lectura_ConDemasiadasEntradas_SeRechaza()
    {
        var entradas = Enumerable.Range(0, ZipCodec.MaxEntradas + 1)
            .Select(i => ($"fichero-{i}.json", "[]"))
            .ToArray();
        using var zip = CrearZip(entradas);

        var accion = () => ZipCodec.Leer(zip);

        accion.Should().Throw<BackupException>()
            .Which.Error.Should().BeOfType<TiendaApi.Api.Errors.ValidationError>(
                "el límite de entradas es un error de formato, no un fallo interno");
    }

    [Test]
    public void Lectura_ConMasDeCincuentaMb_SeRechaza()
    {
        // Un zip bomb: pocos bytes comprimidos que ocupan mucho al descomprimir.
        var grande = new string('a', ZipCodec.MaxBytesDescomprimidos + 1024);
        using var zip = CrearZip(("grande.json", grande));

        var accion = () => ZipCodec.Leer(zip);

        accion.Should().Throw<BackupException>()
            .Which.Error.Should().BeOfType<TiendaApi.Api.Errors.ValidationError>();
    }

    [Test]
    public void Entradas_DevuelveLosNombresCompletos()
    {
        using var zip = CrearZip(("subcarpeta/datos.json", "[]"));

        var entradas = ZipCodec.Leer(zip);

        entradas.Should().ContainSingle().Which.Nombre.Should().Be("subcarpeta/datos.json");
    }
}
