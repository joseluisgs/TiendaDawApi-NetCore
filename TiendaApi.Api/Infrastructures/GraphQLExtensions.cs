using Microsoft.AspNetCore.Builder;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extension methods para GraphiQL UI.
/// </summary>
public static class GraphQLExtensions
{
    /// <summary>
    /// Configura la interfaz GraphiQL para explorar el esquema GraphQL.
    /// </summary>
    public static IApplicationBuilder UseGraphiQL(this IApplicationBuilder app)
    {
        Log.Information("🔍 Configurando GraphiQL UI...");
        var webApp = (WebApplication)app;
        
        webApp.MapGet("/graphiql", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>GraphiQL</title>
    <link href=""https://unpkg.com/graphiql/graphiql.min.css"" rel=""stylesheet"" />
</head>
<body style=""margin: 0;"">
    <div id=""graphiql"" style=""height: 100vh;""></div>
    <script crossorigin src=""https://unpkg.com/react/umd/react.production.min.js""></script>
    <script crossorigin src=""https://unpkg.com/react-dom/umd/react-dom.production.min.js""></script>
    <script crossorigin src=""https://unpkg.com/graphiql/graphiql.min.js""></script>
    <script>
        const fetcher = GraphiQL.createFetcher({ url: '/graphql' });
        ReactDOM.render(
            React.createElement(GraphiQL, { fetcher: fetcher }),
            document.getElementById('graphiql')
        );
    </script>
</body>
</html>");
        });
        
        return app;
    }
}
