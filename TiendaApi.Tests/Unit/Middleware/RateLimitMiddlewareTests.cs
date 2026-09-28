using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Middleware;

namespace TiendaApi.Tests.Unit.Middleware;

/// <summary>
/// Tests unitarios para el rate limiting nativo (RateLimitMiddleware).
/// Cada test usa una ruta única para aislar las particiones entre pruebas.
/// </summary>
public class RateLimitMiddlewareTests
{
    private readonly RateLimitingState _state = new();
    private int _nextCalls;

    private Task Next(HttpContext context)
    {
        _nextCalls++;
        return Task.CompletedTask;
    }

    [SetUp]
    public void Setup() => _nextCalls = 0;

    [TearDown]
    public void TearDown() => _state.Dispose();

    private static DefaultHttpContext CreateContext(string method, string path, string? ip = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = ip is null ? IPAddress.Loopback : IPAddress.Parse(ip);
        return context;
    }

    private async Task<DefaultHttpContext> ExecuteAsync(string method, string path, string? ip = null)
    {
        var middleware = new RateLimitMiddleware(Next, _state);
        var context = CreateContext(method, path, ip);
        await middleware.InvokeAsync(context);
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    [Test]
    public async Task Get_DentroDelLimite_DeberiaPermitirYDevolverCabeceras()
    {
        // Arrange
        var path = $"/api/ratelimit-test/{Guid.NewGuid():N}";

        // Act
        DefaultHttpContext last = null!;
        for (var i = 0; i < 3; i++)
        {
            last = await ExecuteAsync(HttpMethods.Get, path);
        }

        // Assert
        _nextCalls.Should().Be(3);
        last.Response.StatusCode.Should().Be(200);
        last.Response.Headers["RateLimit-Limit"].ToString().Should().Be("100");
        last.Response.Headers["RateLimit-Remaining"].ToString().Should().Be("97");
        int.TryParse(last.Response.Headers["RateLimit-Reset"].ToString(), out var reset).Should().BeTrue();
        reset.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Get_LimiteGeneral_DeberiaDevolver429EnLa101()
    {
        // Arrange
        var path = $"/api/ratelimit-test/{Guid.NewGuid():N}";

        // Act
        for (var i = 0; i < RateLimitConfig.GeneralLimit; i++)
        {
            var ok = await ExecuteAsync(HttpMethods.Get, path);
            ok.Response.StatusCode.Should().Be(200);
        }

        var blocked = await ExecuteAsync(HttpMethods.Get, path);

        // Assert
        _nextCalls.Should().Be(RateLimitConfig.GeneralLimit);
        blocked.Response.StatusCode.Should().Be(429);
    }

    [Test]
    public async Task Post_LimiteEscritura_DeberiaDevolver429EnLa21()
    {
        // Arrange
        var path = $"/api/ratelimit-test/{Guid.NewGuid():N}";

        // Act
        for (var i = 0; i < RateLimitConfig.WriteLimit; i++)
        {
            var ok = await ExecuteAsync(HttpMethods.Post, path);
            ok.Response.StatusCode.Should().Be(200);
        }

        var blocked = await ExecuteAsync(HttpMethods.Post, path);

        // Assert
        _nextCalls.Should().Be(RateLimitConfig.WriteLimit);
        blocked.Response.StatusCode.Should().Be(429);
    }

    [Test]
    public async Task Post_En429_DeberiaDevolverJsonConErrorType()
    {
        // Arrange
        var path = $"/api/ratelimit-test/{Guid.NewGuid():N}";
        for (var i = 0; i < RateLimitConfig.WriteLimit; i++)
        {
            await ExecuteAsync(HttpMethods.Post, path);
        }

        // Act
        var blocked = await ExecuteAsync(HttpMethods.Post, path);
        var body = await ReadBodyAsync(blocked);

        // Assert
        blocked.Response.ContentType.Should().Contain("application/json");
        body.Should().Contain("RateLimitError");
        body.Should().Contain("Demasiadas solicitudes");
        body.Should().Contain("retryAfter");
        body.Should().Contain("timestamp");

        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("errorType").GetString().Should().Be("RateLimitError");
        doc.RootElement.GetProperty("retryAfter").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("limit").GetInt32().Should().Be(RateLimitConfig.WriteLimit);
    }

    [Test]
    public async Task Post_En429_DeberiaTenerRetryAfterPositivo()
    {
        // Arrange
        var path = $"/api/ratelimit-test/{Guid.NewGuid():N}";
        for (var i = 0; i < RateLimitConfig.WriteLimit; i++)
        {
            await ExecuteAsync(HttpMethods.Post, path);
        }

        // Act
        var blocked = await ExecuteAsync(HttpMethods.Post, path);

        // Assert
        blocked.Response.StatusCode.Should().Be(429);
        int.TryParse(blocked.Response.Headers["Retry-After"].ToString(), out var retryAfter).Should().BeTrue();
        retryAfter.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Auth_LimiteDe10_DeberiaDevolver429EnLa11()
    {
        // Arrange - la ruta de auth aplica aunque el verbo no sea POST
        var path = $"/api/v1/auth/ratelimit-{Guid.NewGuid():N}";

        // Act
        DefaultHttpContext last = null!;
        for (var i = 0; i < RateLimitConfig.AuthLimit; i++)
        {
            last = await ExecuteAsync(HttpMethods.Get, path);
        }

        var blocked = await ExecuteAsync(HttpMethods.Get, path);

        // Assert
        _nextCalls.Should().Be(RateLimitConfig.AuthLimit);
        last.Response.Headers["RateLimit-Limit"].ToString().Should().Be("10");
        blocked.Response.StatusCode.Should().Be(429);
    }

    [Test]
    public async Task XForwardedFor_LosClientesDeberianTenerContadoresIndependientes()
    {
        // Arrange - agotar el límite de POST de un cliente
        var path = $"/api/ratelimit-test/{Guid.NewGuid():N}";
        for (var i = 0; i < RateLimitConfig.WriteLimit; i++)
        {
            await ExecuteAsync(HttpMethods.Post, path, ip: "10.0.0.1");
        }

        var blocked = await ExecuteAsync(HttpMethods.Post, path, ip: "10.0.0.1");

        // Act - otro cliente con la misma ruta no debe ver el contador del primero
        var other = await ExecuteAsync(HttpMethods.Post, path, ip: "10.0.0.2");

        // Assert
        blocked.Response.StatusCode.Should().Be(429);
        other.Response.StatusCode.Should().Be(200);
        _nextCalls.Should().Be(RateLimitConfig.WriteLimit + 1);
    }

    [Test]
    public async Task En429_NoDeberiaEjecutarElSiguienteMiddleware()
    {
        // Arrange
        var path = $"/api/ratelimit-test/{Guid.NewGuid():N}";
        for (var i = 0; i < RateLimitConfig.WriteLimit; i++)
        {
            await ExecuteAsync(HttpMethods.Post, path);
        }
        var callsBefore = _nextCalls;

        // Act
        await ExecuteAsync(HttpMethods.Post, path);
        await ExecuteAsync(HttpMethods.Post, path);

        // Assert
        _nextCalls.Should().Be(callsBefore);
    }
}
