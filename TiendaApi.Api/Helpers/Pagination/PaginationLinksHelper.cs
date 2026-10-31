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

            // Base de todos los enlaces: mismo host, puerto y path que la
            // petición entrante; BuildUri sobrescribe después la query de cada
            // enlace con page+size (y el orden, si se pasó).
            var uriBase = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host ?? "localhost",
                Port = request.Host.Port ?? (request.IsHttps ? 443 : 80),
                Path = request.Path.Value ?? "",
                Query = request.QueryString.ToString()
            };

            AgregarEnlacesDeNavegacion(linkHeader, uriBase, pagedResult, sortBy, direction);

            return linkHeader.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Añade los cuatro enlaces de navegación: first/prev solo si no es la
    /// primera página y next/last solo si no es la última. Los dos pares de
    /// condiciones se agrupan aquí (antes vivían intercalados en
    /// CreateLinkHeader) porque los vecinos de una página se deciden juntos.
    /// </summary>
    private static void AgregarEnlacesDeNavegacion<T>(
        System.Text.StringBuilder header,
        UriBuilder uriBase,
        PagedResult<T> pagedResult,
        string? sortBy,
        string? direction)
    {
        var tamanoPagina = pagedResult.PageSize;

        if (pagedResult.Page > 1)
        {
            AppendLink(header, BuildUri(1, tamanoPagina, sortBy, direction, uriBase), "first");
            AppendLink(header, BuildUri(pagedResult.Page - 1, tamanoPagina, sortBy, direction, uriBase), "prev");
        }

        if (pagedResult.Page < pagedResult.TotalPages)
        {
            AppendLink(header, BuildUri(pagedResult.Page + 1, tamanoPagina, sortBy, direction, uriBase), "next");
            AppendLink(header, BuildUri(pagedResult.TotalPages, tamanoPagina, sortBy, direction, uriBase), "last");
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
