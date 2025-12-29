namespace TiendaApi.Common;

/// <summary>
/// Tipo Unit para representar "void" en contextos funcionales
/// 
/// En programación funcional, todas las funciones deben retornar un valor.
/// Unit es el equivalente funcional de void - representa "ausencia de valor significativo"
/// pero permite que el código sea composable.
/// 
/// Equivalente en otros lenguajes:
/// - F#: unit
/// - Scala: Unit
/// - Haskell: ()
/// - Java: Void (clase wrapper) o custom Unit type
/// 
/// Uso típico: Result<Unit, AppError> para operaciones que no retornan datos
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// Instancia singleton de Unit - solo existe un valor posible
    /// </summary>
    public static readonly Unit Value = new();

    public override string ToString() => "()";
}
