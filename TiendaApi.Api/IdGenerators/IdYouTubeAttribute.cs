using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.IdGenerators;

/// <summary>
/// Atributo de validación para propiedades que contienen un identificador estilo YouTube.
/// Se aplica a los DTO de petición y comprueba SOLO el formato: 11 caracteres del alfabeto
/// Base64url (letras, dígitos, `-` y `_`).
///
/// Que el identificador exista ya en base de datos es una comprobación distinta y pertenece
/// al servicio (devuelve conflicto, no error de validación).
///
/// La regla de formato vive una sola vez en <see cref="IdYouTube"/>; este atributo y la
/// regla FluentValidation equivalente delegan en ella para que no puedan divergir.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IdYouTubeAttribute : ValidationAttribute
{
    /// <summary>
    /// Comprueba que el valor sea un identificador válido o que esté vacío.
    /// Un valor nulo o vacío es aceptado porque el identificador puede generarse en el servidor.
    /// </summary>
    /// <param name="value">Valor de la propiedad.</param>
    /// <param name="context">Contexto de validación del modelo.</param>
    /// <returns>Éxito si el valor es nulo, vacío o tiene el formato correcto.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null || string.IsNullOrWhiteSpace((string)value))
            return ValidationResult.Success;

        return IdYouTube.EsValido((string)value)
            ? ValidationResult.Success
            : new ValidationResult(IdYouTube.MensajeFormato);
    }
}
