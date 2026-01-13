using FluentAssertions;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Tests.Unit.Errors;

/// <summary>
/// Tests unitarios exhaustivos para DomainError y ErrorType.
/// </summary>
public class DomainErrorTests
{
    #region NotFound Error Tests

    [Test]
    public void NotFound_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.NotFound("Producto no encontrado");

        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Be("Producto no encontrado");
        error.Details.Should().BeNull();
    }

    [Test]
    public void NotFound_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.NotFound("Recurso no encontrado", "ID: 123");

        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Be("Recurso no encontrado");
        error.Details.Should().Be("ID: 123");
    }

    #endregion

    #region Validation Error Tests

    [Test]
    public void Validation_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.Validation("El nombre es obligatorio");

        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Be("El nombre es obligatorio");
        error.ValidationErrors.Should().BeNull();
    }

    [Test]
    public void Validation_ConErrores_CreaErrorConValidationErrors()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Nombre", new[] { "Requerido", "Mínimo 3 caracteres" } },
            { "Email", new[] { "Formato inválido" } }
        };

        var error = DomainError.Validation("Datos inválidos", validationErrors);

        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Be("Datos inválidos");
        error.ValidationErrors.Should().NotBeNull();
        error.ValidationErrors!.Should().ContainKey("Nombre");
        error.ValidationErrors.Should().ContainKey("Email");
    }

    [Test]
    public void ValidationErrors_PuedeSerNull()
    {
        var error = DomainError.Validation("Error");

        error.ValidationErrors.Should().BeNull();
    }

    [Test]
    public void ValidationErrors_ObtieneValor()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Campo", new[] { "Error1", "Error2" } }
        };

        var error = DomainError.Validation("Error", validationErrors);

        error.ValidationErrors.Should().NotBeNull();
        error.ValidationErrors!.Should().ContainKey("Campo");
        error.ValidationErrors!["Campo"].Should().HaveCount(2);
    }

    [Test]
    public void ValidationErrors_MultiplesCampos()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Nombre", new[] { "Requerido" } },
            { "Email", new[] { "Formato inválido", "Ya existe" } },
            { "Password", new[] { "Mínimo 8 caracteres" } }
        };

        var error = DomainError.Validation("Errores de validación", validationErrors);

        error.ValidationErrors.Should().HaveCount(3);
        error.ValidationErrors!["Nombre"].Should().Contain("Requerido");
        error.ValidationErrors!["Email"].Should().HaveCount(2);
    }

    #endregion

    #region BusinessRule Error Tests

    [Test]
    public void BusinessRule_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.BusinessRule("Stock insuficiente");

        error.Type.Should().Be(ErrorType.BusinessRule);
        error.Message.Should().Be("Stock insuficiente");
    }

    [Test]
    public void BusinessRule_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.BusinessRule("No hay stock", "Stock actual: 0, Solicitado: 5");

        error.Type.Should().Be(ErrorType.BusinessRule);
        error.Details.Should().Be("Stock actual: 0, Solicitado: 5");
    }

    #endregion

    #region Unauthorized Error Tests

    [Test]
    public void Unauthorized_ConMensajePersonalizado_CreaErrorCorrecto()
    {
        var error = DomainError.Unauthorized("Token expirado");

        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Be("Token expirado");
    }

    [Test]
    public void Unauthorized_SinMensaje_UsaMensajePorDefecto()
    {
        var error = DomainError.Unauthorized();

        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Be("No autorizado");
    }

    #endregion

    #region Forbidden Error Tests

    [Test]
    public void Forbidden_ConMensajePersonalizado_CreaErrorCorrecto()
    {
        var error = DomainError.Forbidden("No tienes permisos");

        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Be("No tienes permisos");
    }

    [Test]
    public void Forbidden_SinMensaje_UsaMensajePorDefecto()
    {
        var error = DomainError.Forbidden();

        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Be("Acceso denegado");
    }

    #endregion

    #region Conflict Error Tests

    [Test]
    public void Conflict_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.Conflict("El email ya está registrado");

        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Be("El email ya está registrado");
    }

    [Test]
    public void Conflict_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.Conflict("Recurso duplicado", "Email: user@test.com");

        error.Type.Should().Be(ErrorType.Conflict);
        error.Details.Should().Be("Email: user@test.com");
    }

    #endregion

    #region Internal Error Tests

    [Test]
    public void Internal_ConMensajePersonalizado_CreaErrorCorrecto()
    {
        var error = DomainError.Internal("Error en base de datos");

        error.Type.Should().Be(ErrorType.Internal);
        error.Message.Should().Be("Error en base de datos");
    }

    [Test]
    public void Internal_SinMensaje_UsaMensajePorDefecto()
    {
        var error = DomainError.Internal();

        error.Type.Should().Be(ErrorType.Internal);
        error.Message.Should().Be("Error interno del servidor");
    }

    [Test]
    public void Internal_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.Internal("Error crítico", "Stack trace here");

        error.Type.Should().Be(ErrorType.Internal);
        error.Details.Should().Be("Stack trace here");
    }

    #endregion

    #region ToString Tests

    [Test]
    public void ToString_SinDetalles_RetornaFormatoBasico()
    {
        var error = DomainError.NotFound("No encontrado");

        error.ToString().Should().Be("NotFound: No encontrado");
    }

    [Test]
    public void ToString_ConDetalles_RetornaFormatoCompleto()
    {
        var error = DomainError.NotFound("No encontrado", "ID: 123");

        error.ToString().Should().Be("NotFound: No encontrado - ID: 123");
    }

    [Test]
    public void ToString_MensajeLargo_RetornaCompleto()
    {
        var mensajeLargo = new string('A', 1000);
        var error = DomainError.NotFound(mensajeLargo);

        error.ToString().Should().Contain(mensajeLargo);
    }

    [Test]
    public void ToString_MensajeVacio_RetornaFormato()
    {
        var error = DomainError.NotFound("");

        error.ToString().Should().Be("NotFound: ");
    }

    [Test]
    public void ToString_DetailsConCaracteresEspeciales_RetornaCompleto()
    {
        var error = DomainError.NotFound("Error", "Detalle @#$%ñÑáéíóú");

        error.ToString().Should().Contain("Detalle @#$%ñÑáéíóú");
    }

    #endregion

    #region ErrorType Enum Tests

    [Test]
    public void ErrorType_Enum_TieneValoresCorrectos()
    {
        Enum.GetValues<ErrorType>().Should().Contain(ErrorType.NotFound);
        Enum.GetValues<ErrorType>().Should().Contain(ErrorType.Validation);
        Enum.GetValues<ErrorType>().Should().Contain(ErrorType.BusinessRule);
        Enum.GetValues<ErrorType>().Should().Contain(ErrorType.Unauthorized);
        Enum.GetValues<ErrorType>().Should().Contain(ErrorType.Forbidden);
        Enum.GetValues<ErrorType>().Should().Contain(ErrorType.Conflict);
        Enum.GetValues<ErrorType>().Should().Contain(ErrorType.Internal);
    }

    [Test]
    public void ErrorType_Comparacion_Int_ValoresCorrectos()
    {
        ((int)ErrorType.NotFound).Should().Be(0);
        ((int)ErrorType.Validation).Should().Be(1);
        ((int)ErrorType.BusinessRule).Should().Be(2);
        ((int)ErrorType.Unauthorized).Should().Be(3);
        ((int)ErrorType.Forbidden).Should().Be(4);
        ((int)ErrorType.Conflict).Should().Be(5);
        ((int)ErrorType.Internal).Should().Be(6);
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_MismosErrores_DeberianSerIguales()
    {
        var error1 = DomainError.NotFound("Producto no encontrado");
        var error2 = DomainError.NotFound("Producto no encontrado");

        error1.Equals(error2).Should().BeTrue();
    }

    [Test]
    public void Equals_DiferentesErrores_NoDeberianSerIguales()
    {
        var error1 = DomainError.NotFound("Producto no encontrado");
        var error2 = DomainError.NotFound("Categoría no encontrada");

        error1.Equals(error2).Should().BeFalse();
    }

    [Test]
    public void Equals_DiferentesTipos_DeberianSerDistintos()
    {
        var error1 = DomainError.NotFound("No encontrado");
        var error2 = DomainError.Validation("Error");

        error1.Equals(error2).Should().BeFalse();
    }

    [Test]
    public void GetHashCode_MismosErrores_MismoHashCode()
    {
        var error1 = DomainError.NotFound("Producto no encontrado");
        var error2 = DomainError.NotFound("Producto no encontrado");

        error1.GetHashCode().Should().Be(error2.GetHashCode());
    }

    [Test]
    public void Equals_Null_DeberiaRetornarFalse()
    {
        var error = DomainError.NotFound("Error");

        error.Equals(null).Should().BeFalse();
    }

    [Test]
    public void Equals_Object_DeberiaRetornarFalse()
    {
        var error = DomainError.NotFound("Error");
        var obj = new object();

        error.Equals(obj).Should().BeFalse();
    }

    [Test]
    public void OperatorEquals_MismosErrores_RetornaTrue()
    {
        var error1 = DomainError.NotFound("Error");
        var error2 = DomainError.NotFound("Error");

        (error1 == error2).Should().BeTrue();
    }

    [Test]
    public void OperatorNotEquals_DiferentesErrores_RetornaTrue()
    {
        var error1 = DomainError.NotFound("Error1");
        var error2 = DomainError.NotFound("Error2");

        (error1 != error2).Should().BeTrue();
    }

    #endregion

    #region Record Properties Tests

    [Test]
    public void DomainError_Message_ObtieneValorCorrecto()
    {
        var error = new DomainError("Mensaje de prueba", ErrorType.NotFound);

        error.Message.Should().Be("Mensaje de prueba");
    }

    [Test]
    public void DomainError_Type_ObtieneValorCorrecto()
    {
        var error = new DomainError("Mensaje", ErrorType.BusinessRule);

        error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Test]
    public void DomainError_Details_PuedeSerNull()
    {
        var error = new DomainError("Mensaje", ErrorType.NotFound);

        error.Details.Should().BeNull();
    }

    [Test]
    public void DomainError_Details_ObtieneValor()
    {
        var error = new DomainError("Mensaje", ErrorType.NotFound, "Details here");

        error.Details.Should().Be("Details here");
    }

    #endregion
}
