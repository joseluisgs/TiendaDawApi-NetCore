using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace TiendaApi.Tests.Integration.TestContainers.ErrorShape;

/// <summary>
/// Tests de forma de las respuestas de la API servidas por la aplicación real
/// (<c>WebApplicationFactory</c>): códigos de error 400/401/404/409/429 con su
/// cuerpo JSON, más los endpoints de salud (<c>/health</c>) y versión (<c>/version</c>).
/// Usa los contenedores compartidos del assembly con una base de datos propia.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ErrorShapeApiTests
{
    private const string DatabaseName = "it_error_shape";
    private const string AdminUsuario = "admin";
    private const string AdminPassword = "admin";
    private const string IpRateLimit = "172.16.9.9";

    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = null!;
    private string _adminToken = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connectionString = await AssemblyContainerFixture.CreatePostgresDatabaseAsync(DatabaseName);
        var mongoConnectionString = AssemblyContainerFixture.MongoConnectionString;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", connectionString },
                    { "MongoDbSettings:ConnectionString", mongoConnectionString },
                    { "MongoDbSettings:DatabaseName", DatabaseName }
                });
            });
        });

        _client = _factory.CreateClient();

        var login = await _client.PostAsJsonAsync(
            "/api/v1/auth/signin",
            new { username = AdminUsuario, password = AdminPassword });
        login.StatusCode.Should().Be(HttpStatusCode.OK,
            "el seed de desarrollo debe crear el usuario admin");

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        _adminToken = body.GetProperty("token").GetString()!;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await AssemblyContainerFixture.DropPostgresDatabaseAsync(DatabaseName);
        AssemblyContainerFixture.DropMongoDatabase(DatabaseName);
    }

    [Test]
    public async Task Version_devuelve_200_con_metadatos()
    {
        var response = await _client.GetAsync("/version");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("fileVersion").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("runtime").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("framework").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Health_devuelve_200_con_checks_de_postgres_y_mongo()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("OK");

        var checks = body.GetProperty("checks").EnumerateArray()
            .ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("status").GetString());
        checks.Should().ContainKeys("postgresql", "mongodb");
        checks["postgresql"].Should().Be("OK");
        checks["mongodb"].Should().Be("OK");
    }

    [Test]
    public async Task HealthLive_devuelve_OK_sin_dependencias()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("OK");
    }

    [Test]
    public async Task HealthReady_devuelve_OK_con_dependencias_listas()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("OK");

        var checks = body.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString());
        checks.Should().Contain(new[] { "postgresql", "mongodb" });
    }

    [Test]
    public async Task Recurso_inexistente_devuelve_404_con_shape_de_dominio()
    {
        var response = await _client.GetAsync("/api/categorias/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Endpoint_protegido_sin_token_devuelve_401_con_www_authenticate()
    {
        var response = await _client.GetAsync("/api/pedidos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().NotBeEmpty();
        response.Headers.WwwAuthenticate.ToString().Should().Contain("Bearer");
    }

    [Test]
    public async Task Cuerpo_invalido_devuelve_400_con_problem_details()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/signin",
            new Dictionary<string, string>());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("errors", out var errors).Should().BeTrue(
            "la validación de [ApiController] debe agrupar errores por campo");
        errors.EnumerateObject().Should().NotBeEmpty();
    }

    [Test]
    public async Task Credenciales_invalidas_devuelve_401_con_shape_de_dominio()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/signin",
            new { username = "usuario_que_no_existe", password = "noimporta" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("errorId", out _).Should().BeFalse(
            "los errores de dominio no pasan por el manejador global de excepciones");
    }

    [Test]
    public async Task Recurso_duplicado_devuelve_409_con_shape_de_dominio()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/categorias")
        {
            Content = JsonContent.Create(new { nombre = "Electrónica" })
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "el seed crea la categoría Electrónica, volver a crearla debe dar 409");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Limite_de_peticiones_devuelve_429_con_headers_y_cuerpo()
    {
        using var client = _factory!.CreateClient();
        HttpResponseMessage? rechazada = null;
        HttpResponseMessage? permitida = null;

        for (var i = 0; i < 12 && rechazada is null; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/signin")
            {
                Content = JsonContent.Create(new { username = "sonda_rate_limit", password = "noimporta" })
            };
            request.Headers.Add("X-Forwarded-For", IpRateLimit);

            var response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rechazada = response;
            }
            else
            {
                permitida ??= response;
            }
        }

        rechazada.Should().NotBeNull("el límite de autenticación es 10 peticiones por minuto");
        rechazada!.Headers.RetryAfter.Should().NotBeNull();
        rechazada.Headers.RetryAfter!.Delta.GetValueOrDefault().TotalSeconds.Should().BeGreaterThanOrEqualTo(1);

        var body = await rechazada.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorType").GetString().Should().Be("RateLimitError");
        body.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("path").GetString().Should().Be("/api/v1/auth/signin");
        body.GetProperty("limit").GetInt32().Should().Be(10);
        body.GetProperty("window").GetString().Should().Be("1m");
        body.GetProperty("retryAfter").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        permitida.Should().NotBeNull("al menos una petición debe superar el límite antes del 429");
        permitida!.Headers.Contains("RateLimit-Limit").Should().BeTrue();
        permitida.Headers.Contains("RateLimit-Remaining").Should().BeTrue();
        permitida.Headers.Contains("RateLimit-Reset").Should().BeTrue();
    }
}
