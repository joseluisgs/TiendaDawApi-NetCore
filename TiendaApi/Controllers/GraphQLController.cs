using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;

namespace TiendaApi.Controllers;

/// <summary>
/// GraphQL controller for handling GraphQL queries
/// Endpoint: POST /graphql
/// </summary>
[ApiController]
[Route("[controller]")]
public class GraphQLController : ControllerBase
{
    private readonly IDocumentExecuter _documentExecuter;
    private readonly ISchema _schema;
    private readonly ILogger<GraphQLController> _logger;

    public GraphQLController(
        IDocumentExecuter documentExecuter,
        ISchema schema,
        ILogger<GraphQLController> logger)
    {
        _documentExecuter = documentExecuter;
        _schema = schema;
        _logger = logger;
    }

    /// <summary>
    /// Execute GraphQL query
    /// POST /graphql
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] GraphQLRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { message = "Query is required" });
        }

        // Sanitize query for logging (truncate if too long, remove newlines)
        var sanitizedQuery = request.Query.Replace("\n", " ").Replace("\r", "");
        if (sanitizedQuery.Length > 100)
        {
            sanitizedQuery = sanitizedQuery.Substring(0, 97) + "...";
        }
        _logger.LogInformation("Executing GraphQL query: {Query}", sanitizedQuery);

        var result = await _documentExecuter.ExecuteAsync(options =>
        {
            options.Schema = _schema;
            options.Query = request.Query;
            options.Variables = request.Variables;
            options.OperationName = request.OperationName;
            options.RequestServices = HttpContext.RequestServices;
        });

        if (result.Errors?.Any() == true)
        {
            _logger.LogWarning("GraphQL query errors: {Errors}", result.Errors);
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// GET endpoint for GraphQL queries (for simple queries via URL)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string query, [FromQuery] string? operationName = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Query is required" });
        }

        var request = new GraphQLRequest
        {
            Query = query,
            OperationName = operationName
        };

        return await Post(request);
    }
}

/// <summary>
/// GraphQL request model
/// </summary>
public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public string? OperationName { get; set; }
    public Inputs? Variables { get; set; }
}
