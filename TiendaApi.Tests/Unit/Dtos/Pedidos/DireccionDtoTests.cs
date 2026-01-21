using FluentAssertions;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Tests.Unit.Dtos.Pedidos;

public class DireccionDtoTests
{
    #region Tests de Propiedades

    [Test]
    public void DireccionDto_ConTodosLosCampos_AsignaCorrectamente()
    {
        var dto = new DireccionDto
        {
            Calle = "Gran Vía",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "España",
            CodigoPostal = "28013"
        };

        dto.Calle.Should().Be("Gran Vía");
        dto.Numero.Should().Be("42");
        dto.Ciudad.Should().Be("Madrid");
        dto.Provincia.Should().Be("Madrid");
        dto.Pais.Should().Be("España");
        dto.CodigoPostal.Should().Be("28013");
    }

    [Test]
    public void DireccionDto_PorDefecto_TienePropiedadesVacias()
    {
        var dto = new DireccionDto();

        dto.Calle.Should().BeEmpty();
        dto.Numero.Should().BeNull();
        dto.Ciudad.Should().BeEmpty();
        dto.Provincia.Should().BeNull();
        dto.Pais.Should().BeEmpty();
        dto.CodigoPostal.Should().BeNull();
    }

    [Test]
    public void DireccionDto_PermiteCalleVacia()
    {
        var dto = new DireccionDto { Calle = string.Empty };

        dto.Calle.Should().BeEmpty();
    }

    [Test]
    public void DireccionDto_PermiteValoresMaximos()
    {
        var dto = new DireccionDto
        {
            Calle = new string('A', 200),
            Numero = new string('1', 20),
            Ciudad = new string('C', 100),
            Provincia = new string('P', 100),
            Pais = new string('E', 100),
            CodigoPostal = "12345678901234567890"
        };

        dto.Calle.Length.Should().Be(200);
        dto.Numero.Length.Should().Be(20);
        dto.Ciudad.Length.Should().Be(100);
        dto.Provincia.Length.Should().Be(100);
        dto.Pais.Length.Should().Be(100);
        dto.CodigoPostal.Length.Should().Be(20);
    }

    #endregion

    #region Tests de Serializacion JSON

    [Test]
    public void DireccionDto_Serialize_ConTodosLosCampos()
    {
        var dto = new DireccionDto
        {
            Calle = "Gran Via",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "Espana",
            CodigoPostal = "28013"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(dto);

        json.Should().Contain("calle");
        json.Should().Contain("Gran Via");
        json.Should().Contain("numero");
        json.Should().Contain("42");
        json.Should().Contain("ciudad");
        json.Should().Contain("Madrid");
        json.Should().Contain("provincia");
        json.Should().Contain("pais");
        json.Should().Contain("codigoPostal");
        json.Should().Contain("28013");
    }

    [Test]
    public void DireccionDto_Deserialize_ConTodosLosCampos()
    {
        var json = """
            {
                "calle": "Gran Vía",
                "numero": "42",
                "ciudad": "Madrid",
                "provincia": "Madrid",
                "pais": "España",
                "codigoPostal": "28013"
            }
            """;

        var dto = System.Text.Json.JsonSerializer.Deserialize<DireccionDto>(json);

        dto.Should().NotBeNull();
        dto!.Calle.Should().Be("Gran Vía");
        dto.Numero.Should().Be("42");
        dto.Ciudad.Should().Be("Madrid");
        dto.Provincia.Should().Be("Madrid");
        dto.Pais.Should().Be("España");
        dto.CodigoPostal.Should().Be("28013");
    }

    [Test]
    public void DireccionDto_Deserialize_SinCamposOpcionales()
    {
        var json = """
            {
                "calle": "Calle Principal",
                "ciudad": "Madrid",
                "pais": "España"
            }
            """;

        var dto = System.Text.Json.JsonSerializer.Deserialize<DireccionDto>(json);

        dto.Should().NotBeNull();
        dto!.Calle.Should().Be("Calle Principal");
        dto.Numero.Should().BeNull();
        dto.Ciudad.Should().Be("Madrid");
        dto.Pais.Should().Be("España");
    }

    #endregion

    #region Tests de Direccion Parcial

    [Test]
    public void DireccionDto_SoloCalleYCiudad_EsParcial()
    {
        var dto = new DireccionDto
        {
            Calle = "Calle Test",
            Ciudad = "Barcelona",
            Pais = "España"
        };

        dto.Calle.Should().Be("Calle Test");
        dto.Ciudad.Should().Be("Barcelona");
        dto.Pais.Should().Be("España");
        dto.Numero.Should().BeNull();
    }

    [Test]
    public void DireccionDto_SinCodigoPostal_EsValida()
    {
        var dto = new DireccionDto
        {
            Calle = "Gran Vía",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "España"
        };

        dto.CodigoPostal.Should().BeNull();
    }

    #endregion
}
