using System.Text.RegularExpressions;

namespace TiendaApi.Api.IdGenerators;

/// <summary>
///     Generador de identificadores estilo YouTube: cadenas de exactamente 11 caracteres
///     del alfabeto Base64url (A-Z, a-z, 0-9, `-` y `_`).
///     Un ID de 11 caracteres en Base64url codifica 66 bits (11 × 6), de los que se usan
///     64: 64^11 ≈ 7,4 × 10^19 combinaciones, por lo que la probabilidad de colisión es
///     despreciable. El valor no es correlativo, así que no permite deducir cuándo se creó
///     una entidad vecina ni recorrer el conjunto en orden.
///     La generación no garantiza unicidad, solo la hace muy probable: quien inserta debe
///     dejar que la base de datos rechace la clave duplicada o reintentar con otro valor.
/// </summary>
public static class IdYouTube {
    /// <summary>Alfabeto Base64url: 26 mayúsculas, 26 minúsculas, 10 dígitos, `-` y `_`.</summary>
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    /// <summary>Longitud fija de todos los identificadores generados.</summary>
    public const int Longitud = 11;

    /// <summary>Texto de la expresión regular, para usarlo en atributos de validación.</summary>
    public const string PatronTexto = "[A-Za-z0-9_-]{11}";

    /// <summary>Expresión regular que valida un identificador de esta familia.</summary>
    public static readonly Regex Patron = new($"^{PatronTexto}$", RegexOptions.Compiled);

    /// <summary>Mensaje de error único para cualquier intento de asignar un código con formato inválido.</summary>
    public static readonly string MensajeFormato =
        $"El código debe ser un identificador de {Longitud} caracteres (letras, dígitos, - o _)";

    /// <summary>
    ///     Genera un identificador aleatorio de 11 caracteres.
    ///     <see cref="Random.Shared" /> es thread-safe y evita crear un generador por llamada.
    /// </summary>
    /// <returns>Cadena de 11 caracteres Base64url.</returns>
    public static string Next() {
        return new string([.. Enumerable.Range(0, Longitud).Select(_ => Chars[Random.Shared.Next(Chars.Length)])]);
    }

    /// <summary>
    ///     Indica si un valor tiene la forma válida de estos identificadores.
    /// </summary>
    /// <param name="codigo">Valor a comprobar (puede ser null).</param>
    /// <returns>True si mide 11 caracteres y todos pertenecen al alfabeto.</returns>
    public static bool EsValido(string? codigo) {
        return !string.IsNullOrEmpty(codigo) && Patron.IsMatch(codigo);
    }
}
