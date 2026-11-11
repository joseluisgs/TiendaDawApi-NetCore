using FluentValidation;
using TiendaApi.Api.IdGenerators;

namespace TiendaApi.Api.Validators;

/// <summary>
///     Extensiones de FluentValidation para reglas reutilizables entre validadores.
/// </summary>
public static class ReglasIdentificadorExtensions {
    /// <summary>
    ///     Exige que el valor sea un identificador estilo YouTube (11 caracteres Base64url).
    ///     Delega en <see cref="IdYouTube" />, la misma regla que usa el atributo de validación
    ///     del DTO, para que formato y mensaje no puedan divergir.
    /// </summary>
    /// <param name="rule">Regla del campo que puede quedar vacío.</param>
    /// <typeparam name="T">Tipo del DTO validado.</typeparam>
    /// <returns>Opciones de la regla para encadenar más condiciones.</returns>
    public static IRuleBuilderOptions<T, string?> EsIdentificadorYouTube<T>(this IRuleBuilder<T, string?> rule) {
        return rule.Matches(IdYouTube.Patron).WithMessage(IdYouTube.MensajeFormato);
    }
}
