using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;

namespace TiendaApi.Controllers;

/// <summary>
/// Controlador GraphQL para ejecutar consultas.
/// </summary>
[ApiController]
[Route("[controller]")]
public class GraphQLController : ControllerBase
{
    private readonly IDocumentExecuter _documentExecuter;
    private readonly ISchema _schema;
    private readonly ILogger<GraphQLController> _logger;

    public GraphQLController(IDocumentExecuter documentExecuter, ISchema schema, ILogger<GraphQLController> logger)
    {
        _documentExecuter = documentExecuter;
        _schema = schema;
        _logger = logger;
    }

    /// <summary>
    /// Ejecutar una consulta GraphQL.
    /// POST /graphql
    /// Returns: 200 OK | 400 Bad Request
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] GraphQLRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new { message = "Query es requerida" });

        var sanitizedQuery = request.Query.Replace("\n", " ").Replace("\r", "");
        if (sanitizedQuery.Length > 100)
            sanitizedQuery = sanitizedQuery.Substring(0, 97) + "...";
        
        _logger.LogInformation("Ejecutando consulta GraphQL: {Query}", sanitizedQuery);

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
            _logger.LogWarning("Errores en consulta GraphQL: {Errors}", result.Errors);
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Ejecutar una consulta GraphQL (GET).
    /// GET /graphql?query=...
    /// Returns: 200 OK | 400 Bad Request
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] string query, [FromQuery] string? operationName = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Query es requerida" });

        var request = new GraphQLRequest { Query = query, OperationName = operationName };
        return await Post(request);
    }
}

/// <summary>
/// Modelo de petición GraphQL.
/// </summary>
public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public string? OperationName { get; set; }
    public Inputs? Variables { get; set; }
}
