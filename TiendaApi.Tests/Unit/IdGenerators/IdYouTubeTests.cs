using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using TiendaApi.Api.IdGenerators;

namespace TiendaApi.Tests.Unit.IdGenerators;

/// <summary>
///     Tests unitarios del generador de identificadores estilo YouTube y de su atributo de validación.
/// </summary>
public class IdYouTubeTests {
    #region Generación

    [Test]
    public void Next_DeveGenerarIdentificadorDeOnceCaracteres() {
        string id = IdYouTube.Next();

        id.Should().HaveLength(11);
    }

    [Test]
    public void Next_DeveUsarSoloElAlfabetoBase64Url() {
        var permitidos = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

        for (int i = 0; i < 200; i++) {
            IdYouTube.Next().Should().Match(id => id.All(permitidos.Contains));
        }
    }

    [Test]
    public void Next_DeveGenerarIdentificadoresSinRepetir() {
        var generados = new HashSet<string>();

        for (int i = 0; i < 1000; i++) {
            generados.Add(IdYouTube.Next()).Should().BeTrue($"el identificador {i} se repitió");
        }
    }

    [Test]
    public void Next_DeveMezclarMayusculasYMinusculas() {
        // El alfabeto tiene 64 caracteres: con 1000 muestras es prácticamente imposible
        // que salga solo mayúsculas o solo minúsculas.
        var muestras = Enumerable.Range(0, 1000).Select(_ => IdYouTube.Next()).ToList();

        muestras.Should().Contain(id => id.Any(char.IsUpper));
        muestras.Should().Contain(id => id.Any(char.IsLower));
        muestras.Should().Contain(id => id.Any(char.IsDigit));
    }

    #endregion

    #region Validación de formato

    [Test]
    public void EsValido_ConIdentificadorDeOnceCaracteres_DeveDevolverTrue() {
        IdYouTube.EsValido("XqZsoesa55w").Should().BeTrue();
        IdYouTube.EsValido("aB3-_zY9_Qw").Should().BeTrue();
    }

    [Test]
    public void EsValido_ConValorNuloOVacio_DeveDevolverFalse() {
        IdYouTube.EsValido(null).Should().BeFalse();
        IdYouTube.EsValido("").Should().BeFalse();
    }

    [Test]
    public void EsValido_ConLongitudDistinta_DeveDevolverFalse() {
        IdYouTube.EsValido("abcdefghij").Should().BeFalse(); // 10
        IdYouTube.EsValido("abcdefghijkl").Should().BeFalse(); // 12
    }

    [Test]
    public void EsValido_ConCaracteresFueraDelAlfabeto_DeveDevolverFalse() {
        IdYouTube.EsValido("abc/def?ghi").Should().BeFalse();
        IdYouTube.EsValido("abc defghi1").Should().BeFalse();
    }

    [Test]
    public void Patron_DeveEstarAnclado() {
        // Si el patrón no estuviera anclado, un identificador válido seguido de texto
        // basura pasaría la validación.
        IdYouTube.Patron.IsMatch("abcdefghijkSOBRA").Should().BeFalse();
        IdYouTube.Patron.IsMatch("SOBRAabcdefghijk").Should().BeFalse();
        IdYouTube.Patron.IsMatch("abcdefghijk").Should().BeTrue();
    }

    #endregion

    #region Atributo de validación

    private sealed class Contenedor {
        [IdYouTube] public string? Codigo { get; set; }
    }

    private static ValidationResult? Validar(string? valor) {
        var contexto = new ValidationContext(new Contenedor { Codigo = valor }) {
            MemberName = nameof(Contenedor.Codigo)
        };
        return new IdYouTubeAttribute().GetValidationResult(valor, contexto);
    }

    [Test]
    public void Atributo_ConValorNulo_DebePermitirlo() {
        Validar(null).Should().BeNull();
    }

    [Test]
    public void Atributo_ConValorVacio_DebePermitirlo() {
        // El atributo valida formato; un vacío se trata como "no enviado" y
        // quien recibe el DTO (el validador FluentValidation) decide si lo acepta.
        Validar("").Should().BeNull();
    }

    [Test]
    public void Atributo_ConIdentificadorValido_DebePermitirlo() {
        Validar("XqZsoesa55w").Should().BeNull();
    }

    [Test]
    public void Atributo_ConIdentificadorInvalido_DebeDevolverError() {
        ValidationResult? resultado = Validar("corto");

        resultado.Should().NotBeNull();
        resultado!.ErrorMessage.Should().Be(IdYouTube.MensajeFormato);
    }

    #endregion
}
