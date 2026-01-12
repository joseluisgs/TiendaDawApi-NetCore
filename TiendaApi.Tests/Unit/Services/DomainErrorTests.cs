using FluentAssertions;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Tests.Unit.Services;

/// <summary>
/// Tests unitarios para DomainError.
/// </summary>
public class DomainErrorTests
{
    #region Tests NotFound

    /// <summary>
    /// Dado un mensaje de error, cuando se crea NotFound, entonces tiene tipo NotFound.
    /// Returns: DomainError con Type = NotFound
    /// </summary>
    [Test]
    public void NotFound_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.NotFound("Producto no encontrado");

        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Be("Producto no encontrado");
        error.Details.Should().BeNull();
    }

    /// <summary>
    /// Dado un mensaje y detalles, cuando se crea NotFound, entonces incluye los detalles.
    /// Returns: DomainError con Details
    /// </summary>
    [Test]
    public void NotFound_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.NotFound("Recurso no encontrado", "ID: 123");

        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Be("Recurso no encontrado");
        error.Details.Should().Be("ID: 123");
    }

    #endregion

    #region Tests Validation

    /// <summary>
    /// Dado un mensaje de validación, cuando se crea Validation, entonces tiene tipo Validation.
    /// Returns: DomainError con Type = Validation
    /// </summary>
    [Test]
    public void Validation_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.Validation("El nombre es obligatorio");

        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Be("El nombre es obligatorio");
        error.ValidationErrors.Should().BeNull();
    }

    /// <summary>
    /// Dado errores de validación específicos, cuando se crea Validation, entonces incluye errores.
    /// Returns: DomainError con ValidationErrors
    /// </summary>
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

    #endregion

    #region Tests BusinessRule

    /// <summary>
    /// Dado un mensaje de regla de negocio, cuando se crea BusinessRule, entonces tiene tipo BusinessRule.
    /// Returns: DomainError con Type = BusinessRule
    /// </summary>
    [Test]
    public void BusinessRule_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.BusinessRule("Stock insuficiente");

        error.Type.Should().Be(ErrorType.BusinessRule);
        error.Message.Should().Be("Stock insuficiente");
    }

    /// <summary>
    /// Dado un mensaje y detalles, cuando se crea BusinessRule, entonces incluye los detalles.
    /// Returns: DomainError con Details
    /// </summary>
    [Test]
    public void BusinessRule_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.BusinessRule("No hay stock", "Stock actual: 0, Solicitado: 5");

        error.Type.Should().Be(ErrorType.BusinessRule);
        error.Details.Should().Be("Stock actual: 0, Solicitado: 5");
    }

    #endregion

    #region Tests Unauthorized

    /// <summary>
    /// Dado un mensaje personalizado, cuando se crea Unauthorized, entonces tiene tipo Unauthorized.
    /// Returns: DomainError con Type = Unauthorized
    /// </summary>
    [Test]
    public void Unauthorized_ConMensajePersonalizado_CreaErrorCorrecto()
    {
        var error = DomainError.Unauthorized("Token expirado");

        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Be("Token expirado");
    }

    /// <summary>
    /// Dado ningún mensaje, cuando se crea Unauthorized, entonces usa mensaje por defecto.
    /// Returns: DomainError con mensaje por defecto
    /// </summary>
    [Test]
    public void Unauthorized_SinMensaje_UsaMensajePorDefecto()
    {
        var error = DomainError.Unauthorized();

        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Be("No autorizado");
    }

    #endregion

    #region Tests Forbidden

    /// <summary>
    /// Dado un mensaje personalizado, cuando se crea Forbidden, entonces tiene tipo Forbidden.
    /// Returns: DomainError con Type = Forbidden
    /// </summary>
    [Test]
    public void Forbidden_ConMensajePersonalizado_CreaErrorCorrecto()
    {
        var error = DomainError.Forbidden("No tienes permisos");

        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Be("No tienes permisos");
    }

    /// <summary>
    /// Dado ningún mensaje, cuando se crea Forbidden, entonces usa mensaje por defecto.
    /// Returns: DomainError con mensaje por defecto
    /// </summary>
    [Test]
    public void Forbidden_SinMensaje_UsaMensajePorDefecto()
    {
        var error = DomainError.Forbidden();

        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Be("Acceso denegado");
    }

    #endregion

    #region Tests Conflict

    /// <summary>
    /// Dado un mensaje de conflicto, cuando se crea Conflict, entonces tiene tipo Conflict.
    /// Returns: DomainError con Type = Conflict
    /// </summary>
    [Test]
    public void Conflict_ConMensaje_CreaErrorCorrecto()
    {
        var error = DomainError.Conflict("El email ya está registrado");

        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Be("El email ya está registrado");
    }

    /// <summary>
    /// Dado un mensaje y detalles, cuando se crea Conflict, entonces incluye los detalles.
    /// Returns: DomainError con Details
    /// </summary>
    [Test]
    public void Conflict_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.Conflict("Recurso duplicado", "Email: user@test.com");

        error.Type.Should().Be(ErrorType.Conflict);
        error.Details.Should().Be("Email: user@test.com");
    }

    #endregion

    #region Tests Internal

    /// <summary>
    /// Dado un mensaje personalizado, cuando se crea Internal, entonces tiene tipo Internal.
    /// Returns: DomainError con Type = Internal
    /// </summary>
    [Test]
    public void Internal_ConMensajePersonalizado_CreaErrorCorrecto()
    {
        var error = DomainError.Internal("Error en base de datos");

        error.Type.Should().Be(ErrorType.Internal);
        error.Message.Should().Be("Error en base de datos");
    }

    /// <summary>
    /// Dado ningún mensaje, cuando se crea Internal, entonces usa mensaje por defecto.
    /// Returns: DomainError con mensaje por defecto
    /// </summary>
    [Test]
    public void Internal_SinMensaje_UsaMensajePorDefecto()
    {
        var error = DomainError.Internal();

        error.Type.Should().Be(ErrorType.Internal);
        error.Message.Should().Be("Error interno del servidor");
    }

    /// <summary>
    /// Dado un mensaje y detalles, cuando se crea Internal, entonces incluye los detalles.
    /// Returns: DomainError con Details
    /// </summary>
    [Test]
    public void Internal_ConDetalles_CreaErrorConDetalles()
    {
        var error = DomainError.Internal("Error crítico", "Stack trace here");

        error.Type.Should().Be(ErrorType.Internal);
        error.Details.Should().Be("Stack trace here");
    }

    #endregion

    #region Tests ToString

    /// <summary>
    /// Dado un error sin detalles, cuando se convierte a string, entonces solo incluye tipo y mensaje.
    /// Returns: string formateado
    /// </summary>
    [Test]
    public void ToString_SinDetalles_RetornaFormatoBasico()
    {
        var error = DomainError.NotFound("No encontrado");

        error.ToString().Should().Be("NotFound: No encontrado");
    }

    /// <summary>
    /// Dado un error con detalles, cuando se convierte a string, entonces incluye detalles.
    /// Returns: string formateado con detalles
    /// </summary>
    [Test]
    public void ToString_ConDetalles_RetornaFormatoCompleto()
    {
        var error = DomainError.NotFound("No encontrado", "ID: 123");

        error.ToString().Should().Be("NotFound: No encontrado - ID: 123");
    }

    #endregion

    #region Tests ErrorType Enum

    /// <summary>
    /// Dado el enum ErrorType, cuando se verifican los valores, entonces tienen valores correctos.
    /// Returns: valores del enum
    /// </summary>
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

    #endregion
}
