using Microsoft.AspNetCore.Http;
using TiendaApi.Apis.Dtos.Common;

namespace TiendaApi.Apis.Utils.Pagination;

/// <summary>
/// Utility class for generating RFC 5988 Link headers for pagination.
/// Provides navigation links (first, last, next, prev) for paginated results.
/// </summary>
/// <remarks>
/// <para>
/// This class generates HTTP Link headers following RFC 5988 standard, commonly used
/// in REST APIs for paginated collections. The header format is:
/// <code>&lt;url&gt;; rel="relation"</code>
/// </para>
/// <para>
/// <b>Example header value:</b>
/// <code>&lt;http://api.example.com/products?page=1&amp;size=10&gt;; rel="first", &lt;http://api.example.com/products?page=2&amp;size=10&gt;; rel="next"</code>
/// </para>
/// <para>
/// <b>Relations supported:</b>
/// <list type="bullet">
///   <item><description>first: URL to the first page</description></item>
///   <item><description>last: URL to the last page</description></item>
///   <item><description>next: URL to the next page</description></item>
///   <item><description>prev: URL to the previous page</description></item>
/// </list>
/// </para>
/// </remarks>
public static class PaginationLinksHelper
{
    /// <summary>
    /// Creates a RFC 5988 Link header value for a paginated result.
    /// </summary>
    /// <param name="pagedResult">The paginated result containing pagination metadata.</param>
    /// <param name="request">The current HTTP request to extract the base URL.</param>
    /// <param name="sortBy">The field used for sorting (optional, for URL generation).</param>
    /// <param name="direction">The sort direction, "asc" or "desc" (optional).</param>
    /// <returns>A string containing the RFC 5988 Link header value, or empty string if not applicable.</returns>
    /// <example>
    /// <code>
    /// var header = PaginationLinksHelper.CreateLinkHeader(pagedResult, Request, "nombre", "asc");
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

            int currentPage = pagedResult.Page;
            int totalPages = pagedResult.TotalPages;
            int pageSize = pagedResult.PageSize;

            // first: Always present unless we're on page 1
            if (currentPage > 1)
            {
                var firstUri = BuildUri(1, pageSize, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, firstUri, "first");
            }

            // prev: Present if we're not on page 1
            if (currentPage > 1)
            {
                var prevUri = BuildUri(currentPage - 1, pageSize, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, prevUri, "prev");
            }

            // next: Present if we're not on the last page
            if (currentPage < totalPages)
            {
                var nextUri = BuildUri(currentPage + 1, pageSize, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, nextUri, "next");
            }

            // last: Always present unless we're on the last page
            if (currentPage < totalPages)
            {
                var lastUri = BuildUri(totalPages, pageSize, sortBy, direction, uriBuilder);
                AppendLink(linkHeader, lastUri, "last");
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
