using System.Globalization;
using System.Text;

namespace TiendaApi.Api.Services.Backup;

/// <summary>
/// Codificador CSV según RFC 4180, sin dependencias externas.
///
/// 🎓 Reglas que implementa:
/// - Los campos se separan con coma.
/// - Un campo se entrecomilla si contiene coma, comilla o salto de línea.
/// - Las comillas dentro de un campo entrecomillado se duplican ("").
/// - La lectura acepta finales de línea LF y CRLF.
///
/// Se usa UTF-8 con BOM en la escritura para que Excel abra los acentos bien,
/// y cultura invariante en números y fechas para que el archivo sea legible
/// en cualquier configuración regional.
/// </summary>
public static class CsvCodec
{
    /// <summary>Separador de campos.</summary>
    private const char Separador = ',';

    /// <summary>
    /// Crea un escritor UTF-8 con BOM sobre la salida indicada.
    /// </summary>
    public static StreamWriter CrearEscritor(Stream salida) =>
        new(salida, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), bufferSize: 4096, leaveOpen: true);

    /// <summary>Escribe la línea de cabecera.</summary>
    public static void EscribirCabecera(TextWriter escritor, IReadOnlyList<string> columnas) =>
        escritor.WriteLine(string.Join(Separador, columnas.Select(Escapar)));

    /// <summary>Escribe una fila de valores ya convertidos a texto.</summary>
    public static void EscribirFila(TextWriter escritor, IReadOnlyList<string?> valores) =>
        escritor.WriteLine(string.Join(Separador, valores.Select(v => Escapar(v ?? string.Empty))));

    /// <summary>Formatea un decimal en cultura invariante (punto decimal).</summary>
    public static string Formatear(decimal valor) =>
        valor.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Formatea una fecha en ISO 8601 (UTC).</summary>
    public static string Formatear(DateTime valor) =>
        valor.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>
    /// Lee todas las filas de un CSV. La primera fila suele ser la cabecera.
    /// </summary>
    public static List<string[]> Leer(TextReader lector)
    {
        var filas = new List<string[]>();
        var campo = new StringBuilder();
        var fila = new List<string>();
        var entreComillas = false;

        void CerrarCampo()
        {
            fila.Add(campo.ToString());
            campo.Clear();
        }

        void CerrarFila()
        {
            CerrarCampo();
            // Una línea vacía al final no produce una fila fantasma.
            if (fila.Count > 1 || fila[0].Length > 0)
                filas.Add(fila.ToArray());
            fila = new List<string>();
        }

        int caract;
        while ((caract = lector.Read()) != -1)
        {
            var c = (char)caract;

            if (entreComillas)
            {
                if (c == '"')
                {
                    if (lector.Peek() == '"')
                    {
                        lector.Read();
                        campo.Append('"');
                    }
                    else
                    {
                        entreComillas = false;
                    }
                }
                else
                {
                    campo.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    entreComillas = true;
                    break;
                case Separador:
                    CerrarCampo();
                    break;
                case '\r':
                    if (lector.Peek() == '\n') lector.Read();
                    CerrarFila();
                    break;
                case '\n':
                    CerrarFila();
                    break;
                default:
                    campo.Append(c);
                    break;
            }
        }

        // Última fila si el archivo no termina en salto de línea.
        if (campo.Length > 0 || fila.Count > 0)
            CerrarFila();

        return filas;
    }

    /// <summary>Convierte un texto a decimal con cultura invariante (0 si está vacío).</summary>
    public static decimal? ADecimal(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? null
            : decimal.Parse(texto, NumberStyles.Number, CultureInfo.InvariantCulture);

    /// <summary>Convierte un texto a entero largo (null si está vacío o no es numérico).</summary>
    public static long? AEnteroLargo(string texto) =>
        long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) ? valor : null;

    /// <summary>Convierte un texto a entero (null si está vacío o no es numérico).</summary>
    public static int? AEntero(string texto) =>
        int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) ? valor : null;

    /// <summary>Convierte un texto a booleano ("true"/"1" son verdaderos).</summary>
    public static bool ABooleano(string texto) =>
        texto.Equals("true", StringComparison.OrdinalIgnoreCase) || texto == "1";

    /// <summary>Convierte un texto a fecha ISO 8601 (null si está vacío).</summary>
    public static DateTime? AFecha(string texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? null
            : DateTime.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    /// <summary>Entrecomilla un valor si lo necesita (RFC 4180).</summary>
    private static string Escapar(string valor)
    {
        if (valor.IndexOfAny([Separador, '"', '\r', '\n']) < 0)
            return valor;

        return $"\"{valor.Replace("\"", "\"\"")}\"";
    }
}
