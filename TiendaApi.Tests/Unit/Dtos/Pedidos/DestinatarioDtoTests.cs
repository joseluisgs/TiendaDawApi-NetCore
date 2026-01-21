using FluentAssertions;
using FluentValidation;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Tests.Unit.Dtos.Pedidos;

public class DestinatarioDtoTests
{
    #region Tests de Propiedades

    [Test]
    public void DestinatarioDto_ConTodosLosCampos_AsignaCorrectamente()
    {
        var direccion = new DireccionDto
        {
            Calle = "Gran Vía",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "España",
            CodigoPostal = "28013"
        };

        var dto = new DestinatarioDto
        {
            NombreCompleto = "María García López",
            Email = "maria.garcia@email.com",
            Telefono = "+34612345678",
            Direccion = direccion
        };

        dto.NombreCompleto.Should().Be("María García López");
        dto.Email.Should().Be("maria.garcia@email.com");
        dto.Telefono.Should().Be("+34612345678");
        dto.Direccion.Should().Be(direccion);
    }

    [Test]
    public void DestinatarioDto_PorDefecto_TienePropiedadesVacias()
    {
        var dto = new DestinatarioDto();

        dto.NombreCompleto.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
        dto.Telefono.Should().BeNull();
        dto.Direccion.Should().NotBeNull();
    }

    [Test]
    public void DestinatarioDto_PermiteNombreVacio()
    {
        var dto = new DestinatarioDto { NombreCompleto = string.Empty };

        dto.NombreCompleto.Should().BeEmpty();
    }

    [Test]
    public void DestinatarioDto_PermiteValoresMaximos()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = new string('A', 200),
            Email = new string('a', 244) + "@test.com",  // 244 + 1 (@) + 8 (test.com) = 253
            Telefono = "+12345678901234567890",  // 21 chars - excede el máximo
            Direccion = new DireccionDto { Calle = "Test", Ciudad = "Madrid", Pais = "España" }
        };

        dto.NombreCompleto.Length.Should().Be(200);
        dto.Email.Length.Should().Be(253);
        dto.Telefono!.Length.Should().Be(21);
    }

    #endregion

    #region Tests de Serializacion JSON

    [Test]
    public void DestinatarioDto_Serialize_ConTodosLosCampos()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Maria Garcia Lopez",
            Email = "maria.garcia@email.com",
            Telefono = "+34612345678",
            Direccion = new DireccionDto
            {
                Calle = "Gran Via",
                Numero = "42",
                Ciudad = "Madrid",
                Provincia = "Madrid",
                Pais = "Espana",
                CodigoPostal = "28013"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(dto);

        json.Should().Contain("nombreCompleto");
        json.Should().Contain("email");
        json.Should().Contain("telefono");
        json.Should().Contain("direccion");
    }

    [Test]
    public void DestinatarioDto_Deserialize_ConTodosLosCampos()
    {
        var json = """
            {
                "nombreCompleto": "María García López",
                "email": "maria.garcia@email.com",
                "telefono": "+34612345678",
                "direccion": {
                    "calle": "Gran Vía",
                    "numero": "42",
                    "ciudad": "Madrid",
                    "provincia": "Madrid",
                    "pais": "España",
                    "codigoPostal": "28013"
                }
            }
            """;

        var dto = System.Text.Json.JsonSerializer.Deserialize<DestinatarioDto>(json);

        dto.Should().NotBeNull();
        dto!.NombreCompleto.Should().Be("María García López");
        dto.Email.Should().Be("maria.garcia@email.com");
        dto.Telefono.Should().Be("+34612345678");
        dto.Direccion.Should().NotBeNull();
        dto.Direccion!.Calle.Should().Be("Gran Vía");
        dto.Direccion.CodigoPostal.Should().Be("28013");
    }

    [Test]
    public void DestinatarioDto_Deserialize_SoloConNombre()
    {
        var json = """
            {
                "nombreCompleto": "Juan Pérez",
                "email": "juan@email.com",
                "direccion": {
                    "calle": "Calle Test",
                    "ciudad": "Madrid",
                    "pais": "España"
                }
            }
            """;

        var dto = System.Text.Json.JsonSerializer.Deserialize<DestinatarioDto>(json);

        dto.Should().NotBeNull();
        dto!.NombreCompleto.Should().Be("Juan Pérez");
        dto.Email.Should().Be("juan@email.com");
        dto.Direccion.Should().NotBeNull();
        dto.Direccion!.Calle.Should().Be("Calle Test");
    }

    [Test]
    public void DestinatarioDto_Deserialize_SinDireccion()
    {
        var json = """
            {
                "nombreCompleto": "Juan Pérez",
                "email": "juan@email.com"
            }
            """;

        var dto = System.Text.Json.JsonSerializer.Deserialize<DestinatarioDto>(json);

        dto.Should().NotBeNull();
        dto!.NombreCompleto.Should().Be("Juan Pérez");
        dto.Email.Should().Be("juan@email.com");
        dto.Direccion.Should().NotBeNull();
    }

    #endregion

    #region Tests de Direccion Anidada

    [Test]
    public void DestinatarioDto_SinDireccion_AsignaNuevaDireccion()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User"
        };

        dto.Direccion.Should().NotBeNull();
    }

    [Test]
    public void DestinatarioDto_DireccionVacia_EsValido()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User",
            Direccion = new DireccionDto()
        };

        dto.Direccion.Should().NotBeNull();
        dto.Direccion.Calle.Should().BeEmpty();
    }

    #endregion

    #region Tests de Casos Especiales

    [Test]
    public void DestinatarioDto_SoloConDireccion_TieneCamposVacios()
    {
        var dto = new DestinatarioDto
        {
            Direccion = new DireccionDto
            {
                Calle = "Calle Test",
                Ciudad = "Barcelona",
                Pais = "España"
            }
        };

        dto.NombreCompleto.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
        dto.Direccion.Should().NotBeNull();
    }

    [Test]
    public void DestinatarioDto_CamposOpcionales_SonVerdaderamenteOpcionales()
    {
        var dto = new DestinatarioDto();

        dto.NombreCompleto.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
        dto.Telefono.Should().BeNull();
        dto.Direccion.Should().NotBeNull();
    }

    #endregion
}
