using System.Text.Json;
using FluentAssertions;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TiendaApi.Api.Infrastructures;

namespace TiendaApi.Tests.Unit.GraphQL;

/// <summary>
///     🎓 Test de esquema: construye el esquema REAL de la API en proceso y
///     comprueba sus invariantes vía introspección. Impide que vuelva a aparecer
///     el bug de los "tipos fantasma" (types de entidad presentes en la
///     introspección que ninguna operación puede devolver) y del tipo↔DTO
///     mismatch (campos expuestos que luego no existen en lo que se sirve).
/// </summary>
[TestFixture]
public class SchemaTests {
    private const string IntrospectionQuery = """
                                              {
                                                __schema {
                                                  queryType { fields { name } }
                                                  mutationType { fields { name } }
                                                  subscriptionType { fields { name } }
                                                  types { name kind fields { name type { kind name ofType { kind name ofType { kind name } } } } }
                                                }
                                              }
                                              """;

    private static async Task<JsonElement> GetSchemaAsync() {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Development");

        var services = new ServiceCollection();
        services.AddLogging();
        IRequestExecutorBuilder builder = services.AddGraphQL(environment.Object);
        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IOperationRequest request = OperationRequestBuilder.New()
            .SetDocument(IntrospectionQuery)
            .AllowIntrospection()
            .Build();
        IExecutionResult response = await executor.ExecuteAsync(request);
        string json = response.ToJson();

        using var doc = JsonDocument.Parse(json);
        JsonElement raiz = doc.RootElement;
        if (!raiz.TryGetProperty("data", out JsonElement data) ||
            data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("__schema", out JsonElement schema)) {
            throw new InvalidOperationException($"La introspección no devolvió __schema: {json}");
        }

        return schema.Clone();
    }

    private static string[] NombresTipos(JsonElement schema) {
        return schema.GetProperty("types").EnumerateArray()
            .Select(t => t.GetProperty("name").GetString()!)
            .Where(n => !n.StartsWith("__"))
            .ToArray();
    }

    /// <summary>Nombres de los campos de un tipo, o null si el tipo no existe.</summary>
    private static string[]? CamposDe(JsonElement schema, string nombreTipo) {
        foreach (JsonElement t in schema.GetProperty("types").EnumerateArray()) {
            if (t.GetProperty("name").GetString() != nombreTipo) {
                continue;
            }

            if (!t.TryGetProperty("fields", out JsonElement campos)) {
                return Array.Empty<string>();
            }

            return campos.EnumerateArray().Select(c => c.GetProperty("name").GetString()!).ToArray();
        }

        return null;
    }

    private static string? TipoDeCampo(JsonElement schema, string nombreTipo, string nombreCampo) {
        foreach (JsonElement t in schema.GetProperty("types").EnumerateArray()) {
            if (t.GetProperty("name").GetString() != nombreTipo) {
                continue;
            }

            if (!t.TryGetProperty("fields", out JsonElement campos)) {
                return null;
            }

            foreach (JsonElement c in campos.EnumerateArray()) {
                if (c.GetProperty("name").GetString() != nombreCampo) {
                    continue;
                }

                JsonElement tipo = c.GetProperty("type");
                while (tipo.GetProperty("kind").GetString() is "NON_NULL" or "LIST") {
                    tipo = tipo.GetProperty("ofType");
                }

                return tipo.GetProperty("name").GetString();
            }
        }

        return null;
    }

    [Test]
    public async Task Esquema_NoContieneTiposFantasmaDeEntidad() {
        JsonElement schema = await GetSchemaAsync();
        string[] tipos = NombresTipos(schema);

        // Antes: Producto/Categoria/Proveedor (entidades) estaban en la
        // introspección pero ninguna operación podía devolverlos.
        tipos.Should().NotContain("Producto", "la entidad Producto no debe estar en el esquema (se sirve ProductoDto)");
        tipos.Should().NotContain("Categoria",
            "la entidad Categoria no debe estar en el esquema (se sirve CategoriaDto)");
        tipos.Should().NotContain("Proveedor",
            "la entidad Proveedor no debe estar en el esquema (se sirve ProveedorResumenDto)");
        tipos.Should().Contain(new[] { "ProductoDto", "CategoriaDto", "ProveedorResumenDto" });
    }

    [Test]
    public async Task Esquema_ProductoDto_SoloExponeCamposDelContratoRest() {
        JsonElement schema = await GetSchemaAsync();
        string[]? campos = CamposDe(schema, "ProductoDto");

        campos.Should().NotBeNull();
        campos!.Should().BeEquivalentTo(new[] {
            "id", "nombre", "descripcion", "precio", "stock", "imagen",
            "categoria", "proveedor", "createdAt", "updatedAt"
        }, "los campos de ProductoDto deben ser exactamente los del contrato REST");

        // Campos internos de la entidad que jamás deben consultarse:
        campos.Should().NotContain(new[]
            { "categoriaId", "proveedorId", "isDeleted", "rowVersion", "isLocalImage", "imagenUrl" });
    }

    [Test]
    public async Task Esquema_CategoriaDto_NoExponeNavegacionAProductosNiBorradoLogico() {
        JsonElement schema = await GetSchemaAsync();
        string[]? campos = CamposDe(schema, "CategoriaDto");

        campos.Should().NotBeNull();
        campos!.Should().BeEquivalentTo("id", "nombre", "descripcion", "createdAt", "updatedAt");
        campos.Should().NotContain(new[] { "productos", "isDeleted" },
            "el listado se pide por la query productos, no por navegación; el borrado lógico no se expone");
    }

    [Test]
    public async Task Esquema_Raices_Completas() {
        JsonElement schema = await GetSchemaAsync();

        CamposDeRaiz(schema, "queryType").Should().BeEquivalentTo("productos", "producto", "productosPaged",
            "categorias", "categoria", "categoriasPaged", "pedidos", "misPedidos");

        CamposDeRaiz(schema, "mutationType").Should()
            .BeEquivalentTo("createProducto", "updateProducto", "deleteProducto");

        CamposDeRaiz(schema, "subscriptionType").Should().BeEquivalentTo("onProductoCreado", "onProductoActualizado",
            "onProductoEliminado", "onStockBajo");
    }

    [Test]
    public async Task Esquema_ResumenDeCategoriaYProveedor_LlevanDescripcion() {
        JsonElement schema = await GetSchemaAsync();

        // Ampliación aprobada: el resumen de categoría/proveedor dentro de un
        // producto gana descripcion (aditivo para REST y GraphQL a la vez).
        CamposDe(schema, "CategoriaResumenDto")!.Should().BeEquivalentTo("id", "nombre", "descripcion");
        CamposDe(schema, "ProveedorResumenDto")!.Should().BeEquivalentTo("codigo", "nombre", "descripcion");
    }

    [Test]
    public async Task Esquema_Pedidos_TiposCompletos() {
        JsonElement schema = await GetSchemaAsync();

        CamposDe(schema, "PedidoDto")!.Should().BeEquivalentTo("id", "userId", "destinatario", "items", "total",
            "estado", "direccionEnvio", "createdAt");

        CamposDe(schema, "DestinatarioDto")!.Should()
            .BeEquivalentTo("nombreCompleto", "email", "telefono", "direccion");

        CamposDe(schema, "DireccionDto")!.Should()
            .BeEquivalentTo("calle", "numero", "ciudad", "provincia", "pais", "codigoPostal");

        CamposDe(schema, "PedidoItemDto")!.Should()
            .BeEquivalentTo("productoId", "nombreProducto", "cantidad", "precio", "subtotal");
    }

    [Test]
    public async Task Esquema_ProductoDto_UsaLongParaIds_NoDosConvenciones() {
        JsonElement schema = await GetSchemaAsync();

        // Mismo identificador, una sola convención: Long (como en REST).
        TipoDeCampo(schema, "ProductoDto", "id").Should()
            .Be("Long", "el id del producto debe ser Long (mismo contrato que REST)");
    }

    private static string[] CamposDeRaiz(JsonElement schema, string raiz) {
        return schema.GetProperty(raiz).GetProperty("fields").EnumerateArray()
            .Select(f => f.GetProperty("name").GetString()!)
            .ToArray();
    }
}
