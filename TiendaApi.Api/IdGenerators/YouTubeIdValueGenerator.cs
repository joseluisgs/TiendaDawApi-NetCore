using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace TiendaApi.Api.IdGenerators;

/// <summary>
///     Generador de identificadores estilo YouTube para claves primarias de tipo <c>string</c>
///     en Entity Framework Core. Delega en <see cref="IdYouTube" />.
///     EF Core solo invoca un generador cuando la propiedad llega con el valor por defecto de
///     su tipo; para <c>string</c> ese centinela es <c>null</c>. Si la propiedad llega
///     inicializada (incluso con <c>string.Empty</c>), EF la da por asignada y no genera nada.
///     Por eso la clave se declara sin inicializar en la entidad.
/// </summary>
public sealed class YouTubeIdValueGenerator : ValueGenerator<string> {
    /// <summary>Los identificadores son valores definitivos, no temporales.</summary>
    public override bool GeneratesTemporaryValues => false;

    /// <summary>
    ///     Devuelve el siguiente identificador generado.
    /// </summary>
    /// <param name="entry">Entrada del ChangeTracker que EF Core asocia a la entidad.</param>
    /// <returns>Identificador de 11 caracteres.</returns>
    public override string Next(EntityEntry entry) {
        return IdYouTube.Next();
    }
}
