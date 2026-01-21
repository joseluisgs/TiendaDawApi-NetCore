using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Dtos.Common;

namespace TiendaApi.Api.Helpers.Pagination;

/// <summary>
/// Genera headers de enlace RFC 5988 para paginación.
/// </summary>
/// <example>
/// var header = PaginationLinksHelper.CreateLinkHeader(resultado, Request, "nombre", "asc");
/// // Resultado: &lt;http://api.com/productos?page=1&amp;size=10&gt;; rel="first", &lt;http://api.com/productos?page=2&amp;size=10&gt;; rel="next"
/// </example>
public static class PaginationLinksHelper
{
    /// <summary>
    /// Crea un header Link RFC 5988 para resultados paginados.
    /// </summary>
    /// <param name="pagedResult">Resultado paginado.</param>
    /// <param name="request">Petición HTTP actual.</param>
    /// <param name="sortBy">Campo de ordenación (opcional).</param>
    /// <param name="direction">Dirección de ordenación (opcional).</param>
    /// <returns>Header Link o cadena vacía.</returns>
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

            if (paginaActual > 1)
            {
                var uriFirst = BuildUri(1, tamanoPagina, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, uriFirst, "first");
            }

            if (paginaActual > 1)
            {
                var uriPrev = BuildUri(paginaActual - 1, tamanoPagina, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, uriPrev, "prev");
            }

            if (paginaActual < totalPaginas)
            {
                var uriNext = BuildUri(paginaActual + 1, tamanoPagina, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, uriNext, "next");
            }

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
