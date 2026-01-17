using FluentAssertions;
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.GraphQL;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Services.Productos;

namespace TiendaApi.Tests.Integration.GraphQL;

[TestFixture]
public class GraphQLIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private HttpClient _httpClient = null!;
    private TiendaDbContext _dbContext = null!;
    private long _categoriaId;
    private long _productoId;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("tienda_graphql_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgresContainer.StartAsync();

        var connectionString = _postgresContainer.GetConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IProductoService, ProductoService>();

        var serviceProvider = services.BuildServiceProvider();

        _dbContext = serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        // Crear categoría de prueba
        var categoriaRepo = serviceProvider.GetRequiredService<ICategoriaRepository>();
        var categoria = new Categoria { Nombre = "Electrónica Test" };
        await categoriaRepo.SaveAsync(categoria);
        _categoriaId = categoria.Id;

        // Configurar HttpClient para la API
        var appFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<TiendaDbContext>(options =>
                    options.UseNpgsql(connectionString));
                services.AddScoped<ICategoriaRepository, CategoriaRepository>();
                services.AddScoped<IProductoRepository, ProductoRepository>();
                services.AddScoped<IProductoService, ProductoService>();
            });
        });

        _httpClient = appFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        await _dbContext?.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private async Task<string> GetAdminToken()
    {
        // Login para obtener token JWT
        var loginResponse = await _httpClient.PostAsJsonAsync("/api/v1/auth/signin", new
        {
            username = "admin",
            password = "Admin1234"
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return loginResult.GetProperty("token").GetString()!;
    }

    [Test]
    public async Task GraphQL_QueryProductos_WithoutAuth_ReturnsProducts()
    {
        // Arrange
        var query = """
            query {
                productos {
                    id
                    nombre
                    precio
                    stock
                }
            }
            """;

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("data").GetProperty("productos").EnumerateArray().Should().NotBeNull();
    }

    [Test]
    public async Task GraphQL_QueryCategorias_WithoutAuth_ReturnsCategorias()
    {
        // Arrange
        var query = """
            query {
                categorias {
                    id
                    nombre
                }
            }
            """;

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var categorias = content.GetProperty("data").GetProperty("categorias").EnumerateArray();
        categorias.Should().Contain(c => c.GetProperty("nombre").GetString() == "Electrónica Test");
    }

    [Test]
    public async Task GraphQL_MutationCreateProducto_WithoutAuth_ReturnsError()
    {
        // Arrange
        var mutation = """
            mutation($input: CreateProductoInput!) {
                createProducto(input: $input) {
                    id
                    nombre
                }
            }
            """;
        var variables = new
        {
            input = new
            {
                nombre = "Test Product",
                precio = 99.99,
                stock = 10,
                categoriaId = _categoriaId
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query = mutation,
            variables
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();
    }

    [Test]
    public async Task GraphQL_MutationCreateProducto_WithAdminToken_ReturnsSuccess()
    {
        // Arrange
        var token = await GetAdminToken();
        var mutation = """
            mutation($input: CreateProductoInput!) {
                createProducto(input: $input) {
                    id
                    nombre
                    precio
                    stock
                }
            }
            """;
        var variables = new
        {
            input = new
            {
                nombre = "GraphQL Test Product",
                descripcion = "Created via GraphQL",
                precio = 149.99,
                stock = 25,
                categoriaId = _categoriaId
            }
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query = mutation,
            variables
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        var result = content.GetProperty("data").GetProperty("createProducto");
        result.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
        result.GetProperty("nombre").GetString().Should().Be("GraphQL Test Product");
        result.GetProperty("precio").GetDecimal().Should().Be(149.99m);

        _productoId = result.GetProperty("id").GetInt64();

        // Cleanup
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task GraphQL_MutationUpdateProducto_WithAdminToken_ReturnsUpdated()
    {
        // Arrange - First create a product
        var token = await GetAdminToken();
        
        var createMutation = """
            mutation($input: CreateProductoInput!) {
                createProducto(input: $input) {
                    id
                }
            }
            """;
        var createVariables = new
        {
            input = new
            {
                nombre = "Product To Update",
                precio = 99.99,
                stock = 10,
                categoriaId = _categoriaId
            }
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query = createMutation,
            variables = createVariables
        });
        var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productoId = createContent.GetProperty("data").GetProperty("createProducto").GetProperty("id").GetInt64();

        // Now update it
        var updateMutation = """
            mutation($id: Long!, $input: UpdateProductoInput!) {
                updateProducto(id: $id, input: $input) {
                    id
                    nombre
                    precio
                }
            }
            """;
        var updateVariables = new
        {
            id = productoId,
            input = new
            {
                nombre = "Updated via GraphQL",
                precio = 79.99
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query = updateMutation,
            variables = updateVariables
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        var result = content.GetProperty("data").GetProperty("updateProducto");
        result.GetProperty("nombre").GetString().Should().Be("Updated via GraphQL");
        result.GetProperty("precio").GetDecimal().Should().Be(79.99m);

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task GraphQL_MutationDeleteProducto_WithAdminToken_ReturnsTrue()
    {
        // Arrange - First create a product
        var token = await GetAdminToken();
        
        var createMutation = """
            mutation($input: CreateProductoInput!) {
                createProducto(input: $input) {
                    id
                }
            }
            """;
        var createVariables = new
        {
            input = new
            {
                nombre = "Product To Delete",
                precio = 49.99,
                stock = 5,
                categoriaId = _categoriaId
            }
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query = createMutation,
            variables = createVariables
        });
        var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productoId = createContent.GetProperty("data").GetProperty("createProducto").GetProperty("id").GetInt64();

        // Now delete it
        var deleteMutation = """
            mutation($id: Long!) {
                deleteProducto(id: $id)
            }
            """;

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query = deleteMutation,
            variables = new { id = productoId }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        var result = content.GetProperty("data").GetProperty("deleteProducto");
        result.GetBoolean().Should().BeTrue();

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task GraphQL_QueryWithVariables_ReturnsFilteredResults()
    {
        // Arrange
        var query = """
            query($id: Long!) {
                producto(id: $id) {
                    id
                    nombre
                    categoria {
                        nombre
                    }
                }
            }
            """;

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query,
            variables = new { id = _categoriaId }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        var producto = content.GetProperty("data").GetProperty("producto");
        producto.GetProperty("id").GetInt64().Should().Be(_categoriaId);
    }

    [Test]
    public async Task GraphQL_MutationWithInvalidData_ReturnsValidationError()
    {
        // Arrange
        var token = await GetAdminToken();
        var mutation = """
            mutation($input: CreateProductoInput!) {
                createProducto(input: $input) {
                    id
                }
            }
            """;
        var variables = new
        {
            input = new
            {
                nombre = "", // Invalid - empty
                precio = -100, // Invalid - negative
                stock = 10,
                categoriaId = _categoriaId
            }
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/graphql", new
        {
            query = mutation,
            variables
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}
