using Microsoft.AspNetCore.Http;
using TiendaApi.Apis.Dtos.Common;

namespace TiendaApi.Apis.Utils.Helpers.Pagination;

/// <summary>
/// Clase utilitaria para generar headers de enlace RFC 5988 para paginación.
/// Proporciona enlaces de navegación (first, last, next, prev) para resultados paginados.
/// </summary>
/// <remarks>
/// <para>
/// Esta clase genera headers HTTP Link siguiendo el estándar RFC 5988, comúnmente usado
/// en APIs REST para colecciones paginadas. El formato del header es:
/// <code>&lt;url&gt;; rel="relacion"</code>
/// </para>
/// <para>
/// <b>Ejemplo de valor de header:</b>
/// <code>&lt;http://api.ejemplo.com/productos?page=1&amp;size=10&gt;; rel="first", &lt;http://api.ejemplo.com/productos?page=2&amp;size=10&gt;; rel="next"</code>
/// </para>
/// <para>
/// <b>Relaciones soportadas:</b>
/// <list type="bullet">
///   <item><description>first: URL de la primera página</description></item>
///   <item><description>last: URL de la última página</description></item>
///   <item><description>next: URL de la página siguiente</description></item>
///   <item><description>prev: URL de la página anterior</description></item>
/// </list>
/// </para>
/// </remarks>
public static class PaginationLinksHelper
{
    /// <summary>
    /// Crea un valor de header Link RFC 5988 para un resultado paginado.
    /// </summary>
    /// <param name="pagedResult">El resultado paginado que contiene los metadatos de paginación.</param>
    /// <param name="request">La petición HTTP actual para extraer la URL base.</param>
    /// <param name="sortBy">Campo utilizado para ordenar (opcional, para generación de URL).</param>
    /// <param name="direction">Dirección de ordenación, "asc" o "desc" (opcional).</param>
    /// <returns>Una cadena con el valor del header Link RFC 5988, o cadena vacía si no aplica.</returns>
    /// <example>
    /// <code>
    /// var header = PaginationLinksHelper.CreateLinkHeader(resultadoPaginado, Request, "nombre", "asc");
    /// Response.Headers.Append("Link", header);
    /// </code>
    /// </example>
    public static string CreateLinkHeader<T>(PagedResult<T> pagedResult, HttpRequest request, string? sortBy = null, string? direction = null)
    {
        if (pagedResult.TotalPages <= 1)
            return string.Empty;

        try
        {
            var linkHeader = new System.Text.StringBuilder();
            var host = request.Host.Host ?? "localhost";
            var port = request.Host.Port ?? (request.IsHttps ? 443 : 80);
            var scheme = request.Scheme;
            var path = request.Path.Value ?? "";
            var query = request.QueryString.ToString();

            var uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = host,
                Port = port,
                Path = path,
                Query = query
            };

            int paginaActual = pagedResult.Page;
            int totalPaginas = pagedResult.TotalPages;
            int tamanoPagina = pagedResult.PageSize;

            // first: Siempre presente excepto si estamos en la página 1
            if (paginaActual > 1)
            {
                var uriFirst = BuildUri(1, tamanoPagina, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, uriFirst, "first");
            }

            // prev: Presente si no estamos en la página 1
            if (paginaActual > 1)
            {
                var uriPrev = BuildUri(paginaActual - 1, tamanoPagina, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, uriPrev, "prev");
            }

            // next: Presente si no estamos en la última página
            if (paginaActual < totalPaginas)
            {
                var uriNext = BuildUri(paginaActual + 1, tamanoPagina, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, uriNext, "next");
            }

            // last: Siempre presente excepto si estamos en la última página
            if (paginaActual < totalPaginas)
            {
                var uriLast = BuildUri(totalPaginas, tamanoPagina, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, uriLast, "last");
            }

            return linkHeader.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BuildUri(int page, int size, string? sortBy, string? direction, UriBuilder uriBuilder)
    {
        var queryParams = new List<string>();
        queryParams.Add($"page={page}");
        queryParams.Add($"size={size}");

        if (!string.IsNullOrEmpty(sortBy))
        {
            queryParams.Add($"sortBy={sortBy}");
            if (!string.IsNullOrEmpty(direction))
                queryParams.Add($"direction={direction}");
        }

        var query = string.Join("&", queryParams);
        var builder = new UriBuilder
        {
            Scheme = uriBuilder.Scheme,
            Host = uriBuilder.Host,
            Port = uriBuilder.Port,
            Path = uriBuilder.Path,
            Query = query
        };

        return builder.ToString();
    }

    private static void AppendLink(System.Text.StringBuilder header, string uri, string rel)
    {
        if (header.Length > 0)
            header.Append(", ");

        header.Append($"<{uri}>; rel=\"{rel}\"");
    }
}
