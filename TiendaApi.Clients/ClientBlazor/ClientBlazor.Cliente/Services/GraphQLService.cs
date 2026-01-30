using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.State;
using ClientBlazor.Cliente.DTOs.GraphQL;
using CSharpFunctionalExtensions;
using System.Text.Json;

namespace ClientBlazor.Cliente.Services;

/// <summary>
/// Servicio GraphQL simulado - simula operaciones GraphQL sin conectar realmente.
/// Usa Railway Oriented Programming con Result<T,E>.
/// Queries públicas, Mutations requieren autenticación.
/// Soporta subscriptions simuladas.
/// </summary>
public class GraphQLService(
    /// <summary>
    /// Store de autenticación para validar tokens en mutations.
    /// </summary>
    AuthStore authStore)
{
    private readonly Random _random = new();
    private readonly List<GraphQLProductoSimulado> _productos = new()
    {
        new GraphQLProductoSimulado(1, "iPhone 15 Pro", "Teléfono móvil Apple con chip A17", 1199.99m, "iphone15.jpg", 1),
        new GraphQLProductoSimulado(2, "Samsung Galaxy S24 Ultra", "Teléfono Android premium con S Pen", 1299.99m, "s24ultra.jpg", 1),
        new GraphQLProductoSimulado(3, "MacBook Pro 16\"", "Ordenador portátil Apple M3 Pro", 2499.99m, "macbook16.jpg", 2),
        new GraphQLProductoSimulado(4, "Dell XPS 13", "Ultrabook Windows premium", 1499.99m, "xps13.jpg", 2),
        new GraphQLProductoSimulado(5, "Sony WH-1000XM5", "Auriculares inalámbricos con cancelación", 399.99m, "sonywh.jpg", 3),
        new GraphQLProductoSimulado(6, "AirPods Pro", "Auriculares Apple con cancelación", 249.99m, "airpods.jpg", 3),
        new GraphQLProductoSimulado(7, "iPad Air", "Tableta Apple con Apple Pencil", 599.99m, "ipadair.jpg", 4),
        new GraphQLProductoSimulado(8, "Samsung Galaxy Tab S9", "Tableta Android premium", 799.99m, "tabs9.jpg", 4)
    };

    private readonly List<GraphQLCategoriaSimulada> _categorias = new()
    {
        new GraphQLCategoriaSimulada(1, "Smartphones", "Teléfonos móviles y smartphones"),
        new GraphQLCategoriaSimulada(2, "Portátiles", "Ordenadores portátiles y ultrabooks"),
        new GraphQLCategoriaSimulada(3, "Audio", "Auriculares y altavoces"),
        new GraphQLCategoriaSimulada(4, "Tabletas", "Tabletas y dispositivos táctiles")
    };

    /// <summary>
    /// Ejecuta una query GraphQL (operaciones de lectura públicas).
    /// </summary>
    /// <param name="query">La query GraphQL a ejecutar.</param>
    /// <param name="variables">Variables de la query (opcional).</param>
    /// <returns>Resultado de la ejecución.</returns>
    public async Task<Result<GraphQLResult<object>, DomainError>> ExecuteQueryAsync(string query, Dictionary<string, object>? variables = null)
    {
        try
        {
            await Task.Delay(_random.Next(100, 400)); // Simular latencia

            // Sin errores aleatorios para queries (operaciones de lectura)
            // if (_random.Next(100) < 0.5)
            //     return Result.Failure<GraphQLResult<object>, DomainError>(NetworkErrors.ServerError);

            // Parsear la query y ejecutar la operación correspondiente
            if (query.Contains("productos {") && !query.Contains("producto("))
            {
                return ExecuteProductosQuery();
            }
            else if (query.Contains("producto(") || query.Contains("producto(id:"))
            {
                var id = ExtractIdFromQuery(query);
                return ExecuteProductoByIdQuery(id);
            }
            else if (query.Contains("categorias {"))
            {
                return ExecuteCategoriasQuery();
            }
            else if (query.Contains("categoria(") || query.Contains("categoria(id:"))
            {
                var id = ExtractIdFromQuery(query);
                return ExecuteCategoriaByIdQuery(id);
            }
            else
            {
                return Result.Failure<GraphQLResult<object>, DomainError>(ValidationErrors.InvalidEmail);
            }
        }
        catch (Exception)
        {
            return Result.Failure<GraphQLResult<object>, DomainError>(NetworkErrors.ServerError);
        }
    }

    /// <summary>
    /// Ejecuta una mutation GraphQL (operaciones de escritura que requieren JWT).
    /// </summary>
    /// <param name="mutation">La mutation GraphQL a ejecutar.</param>
    /// <returns>Resultado de la ejecución.</returns>
    public async Task<Result<GraphQLResult<object>, DomainError>> ExecuteMutationAsync(string mutation)
    {
        // Validar autenticación para mutations
        if (!authStore.GetState().IsAuthenticated)
            return Result.Failure<GraphQLResult<object>, DomainError>(AuthErrors.LoginRequired);

        if (string.IsNullOrEmpty(authStore.GetState().Token))
            return Result.Failure<GraphQLResult<object>, DomainError>(AuthErrors.TokenExpired);

        try
        {
            await Task.Delay(_random.Next(200, 600)); // Simular latencia

            // Simular error aleatorio (2% de probabilidad)
            if (_random.Next(100) < 2)
                return Result.Failure<GraphQLResult<object>, DomainError>(NetworkErrors.ServerError);

            if (mutation.Contains("createProducto"))
            {
                return ExecuteCreateProductoMutation(mutation);
            }
            else if (mutation.Contains("updateProducto"))
            {
                return ExecuteUpdateProductoMutation(mutation);
            }
            else if (mutation.Contains("deleteProducto"))
            {
                return ExecuteDeleteProductoMutation(mutation);
            }
            else
            {
                return Result.Failure<GraphQLResult<object>, DomainError>(ValidationErrors.InvalidEmail);
            }
        }
        catch (Exception)
        {
            return Result.Failure<GraphQLResult<object>, DomainError>(NetworkErrors.ServerError);
        }
    }

    /// <summary>
    /// Simula una subscription GraphQL (eventos en tiempo real).
    /// </summary>
    /// <param name="subscriptionName">Nombre de la subscription.</param>
    /// <returns>Flujo de eventos simulados.</returns>
    public async IAsyncEnumerable<object> SubscribeAsync(string subscriptionName)
    {
        var random = new Random();
        var eventCount = 0;

        while (true)
        {
            // Eventos más frecuentes al inicio, luego se espacian
            var delay = Math.Max(1000, random.Next(2000, 5000) - (eventCount * 100));
            await Task.Delay(delay);

            eventCount++;

            switch (subscriptionName)
            {
                case "onProductoCreado":
                    // Simular creación de un nuevo producto
                    var nuevoProducto = new GraphQLProductoSimulado(
                        _productos.Max(p => p.Id) + 1,
                        $"Nuevo Producto {eventCount}",
                        $"Producto creado automáticamente #{eventCount}",
                        random.Next(50, 500),
                        $"producto{eventCount}.jpg",
                        (long)random.Next(1, 5)
                    );
                    _productos.Add(nuevoProducto); // Agregarlo a la lista para consistencia

                    yield return new ProductoCreadoEvent(
                        Producto: MapToGraphQLProducto(nuevoProducto),
                        Timestamp: DateTime.UtcNow
                    );
                    break;

                case "onProductoActualizado":
                    if (_productos.Count > 0)
                    {
                        var productoActualizado = _productos[random.Next(_productos.Count)];
                        // Simular actualización de stock
                        productoActualizado = new GraphQLProductoSimulado(
                            productoActualizado.Id,
                            productoActualizado.Nombre,
                            productoActualizado.Descripcion,
                            productoActualizado.Precio,
                            productoActualizado.ImagenUrl,
                            productoActualizado.CategoriaId
                        );

                        yield return new ProductoActualizadoEvent(
                            Producto: MapToGraphQLProducto(productoActualizado),
                            Timestamp: DateTime.UtcNow
                        );
                    }
                    break;

                case "onProductoEliminado":
                    if (_productos.Count > 1) // Mantener al menos un producto
                    {
                        var productoEliminado = _productos[random.Next(_productos.Count)];
                        yield return new ProductoEliminadoEvent(
                            ProductoId: productoEliminado.Id,
                            Timestamp: DateTime.UtcNow
                        );
                        // Nota: En una implementación real, aquí se eliminaría el producto
                    }
                    break;

                case "onStockBajo":
                    if (_productos.Count > 0)
                    {
                        var productoStockBajo = _productos[random.Next(_productos.Count)];
                        var stockBajo = random.Next(1, 6); // 1-5 unidades

                        yield return new StockBajoEvent(
                            ProductoId: productoStockBajo.Id,
                            NombreProducto: productoStockBajo.Nombre,
                            StockActual: stockBajo,
                            Timestamp: DateTime.UtcNow
                        );
                    }
                    break;
            }
        }
    }

    // Métodos auxiliares para queries
    private Result<GraphQLResult<object>, DomainError> ExecuteProductosQuery()
    {
        var productos = _productos.Select(MapToGraphQLProducto).ToList();
        return Result.Success<GraphQLResult<object>, DomainError>(
            new GraphQLResult<object>(Data: new { productos }, Errors: null)
        );
    }

    private Result<GraphQLResult<object>, DomainError> ExecuteProductoByIdQuery(long id)
    {
        var producto = _productos.FirstOrDefault(p => p.Id == id);
        if (producto == null)
        {
            return Result.Success<GraphQLResult<object>, DomainError>(
                new GraphQLResult<object>(
                    Data: new { producto = (GraphQLProductoDto?)null },
                    Errors: new List<GraphQLError> { new GraphQLError("Producto no encontrado", "NOT_FOUND") }
                )
            );
        }

        return Result.Success<GraphQLResult<object>, DomainError>(
            new GraphQLResult<object>(Data: new { producto = MapToGraphQLProducto(producto) }, Errors: null)
        );
    }

    private Result<GraphQLResult<object>, DomainError> ExecuteCategoriasQuery()
    {
        var categorias = _categorias.Select(MapToGraphQLCategoria).ToList();
        return Result.Success<GraphQLResult<object>, DomainError>(
            new GraphQLResult<object>(Data: new { categorias }, Errors: null)
        );
    }

    private Result<GraphQLResult<object>, DomainError> ExecuteCategoriaByIdQuery(long id)
    {
        var categoria = _categorias.FirstOrDefault(c => c.Id == id);
        if (categoria == null)
        {
            return Result.Success<GraphQLResult<object>, DomainError>(
                new GraphQLResult<object>(
                    Data: new { categoria = (GraphQLCategoriaDto?)null },
                    Errors: new List<GraphQLError> { new GraphQLError("Categoría no encontrada", "NOT_FOUND") }
                )
            );
        }

        var productosDeCategoria = _productos.Where(p => p.CategoriaId == id).Select(MapToGraphQLProducto).ToList();
        var categoriaConProductos = new
        {
            categoria.Id,
            categoria.Nombre,
            categoria.Descripcion,
            categoria.CreatedAt,
            categoria.UpdatedAt,
            productos = productosDeCategoria
        };

        return Result.Success<GraphQLResult<object>, DomainError>(
            new GraphQLResult<object>(Data: new { categoria = categoriaConProductos }, Errors: null)
        );
    }

    // Métodos auxiliares para mutations
    private Result<GraphQLResult<object>, DomainError> ExecuteCreateProductoMutation(string mutation)
    {
        var input = ExtractInputFromMutation<CreateProductoInput>(mutation, "createProducto");
        if (input == null)
            return Result.Failure<GraphQLResult<object>, DomainError>(ValidationErrors.EmptyField("input"));

        // Validar datos
        if (string.IsNullOrWhiteSpace(input.Nombre))
            return Result.Failure<GraphQLResult<object>, DomainError>(ValidationErrors.EmptyField("nombre"));

        if (input.Precio <= 0)
            return Result.Failure<GraphQLResult<object>, DomainError>(ValidationErrors.InvalidEmail); // Reutilizando error

        var newId = _productos.Max(p => p.Id) + 1;
        var nuevoProducto = new GraphQLProductoSimulado(
            newId,
            input.Nombre,
            input.Descripcion,
            input.Precio,
            input.Imagen,
            input.CategoriaId
        );

        _productos.Add(nuevoProducto);

        return Result.Success<GraphQLResult<object>, DomainError>(
            new GraphQLResult<object>(Data: new { createProducto = MapToGraphQLProducto(nuevoProducto) }, Errors: null)
        );
    }

    private Result<GraphQLResult<object>, DomainError> ExecuteUpdateProductoMutation(string mutation)
    {
        var id = ExtractIdFromMutation(mutation);
        var input = ExtractInputFromMutation<UpdateProductoInput>(mutation, "updateProducto");
        if (input == null)
            return Result.Failure<GraphQLResult<object>, DomainError>(ValidationErrors.EmptyField("input"));

        var producto = _productos.FirstOrDefault(p => p.Id == id);
        if (producto == null)
            return Result.Failure<GraphQLResult<object>, DomainError>(NetworkErrors.NotFound);

        // Actualizar campos proporcionados
        if (!string.IsNullOrWhiteSpace(input.Nombre)) producto.Nombre = input.Nombre;
        if (!string.IsNullOrWhiteSpace(input.Descripcion)) producto.Descripcion = input.Descripcion;
        if (input.Precio.HasValue) producto.Precio = input.Precio.Value;
        if (input.CategoriaId.HasValue) producto.CategoriaId = input.CategoriaId.Value;
        if (!string.IsNullOrWhiteSpace(input.Imagen)) producto.ImagenUrl = input.Imagen;

        return Result.Success<GraphQLResult<object>, DomainError>(
            new GraphQLResult<object>(Data: new { updateProducto = MapToGraphQLProducto(producto) }, Errors: null)
        );
    }

    private Result<GraphQLResult<object>, DomainError> ExecuteDeleteProductoMutation(string mutation)
    {
        var id = ExtractIdFromMutation(mutation);
        var producto = _productos.FirstOrDefault(p => p.Id == id);
        if (producto == null)
            return Result.Failure<GraphQLResult<object>, DomainError>(NetworkErrors.NotFound);

        _productos.Remove(producto);
        return Result.Success<GraphQLResult<object>, DomainError>(
            new GraphQLResult<object>(Data: new { deleteProducto = true }, Errors: null)
        );
    }

    // Métodos auxiliares
    private GraphQLProductoDto MapToGraphQLProducto(GraphQLProductoSimulado p)
    {
        var categoria = _categorias.FirstOrDefault(c => c.Id == p.CategoriaId);
        return new GraphQLProductoDto(
            Id: p.Id,
            Nombre: p.Nombre,
            Descripcion: p.Descripcion,
            Precio: p.Precio,
            Stock: _random.Next(1, 100),
            Imagen: p.ImagenUrl,
            Categoria: categoria != null ? MapToGraphQLCategoria(categoria) : null,
            CreatedAt: DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
            UpdatedAt: DateTime.UtcNow.AddHours(-_random.Next(0, 24))
        );
    }

    private GraphQLCategoriaDto MapToGraphQLCategoria(GraphQLCategoriaSimulada c)
    {
        return new GraphQLCategoriaDto(
            Id: c.Id,
            Nombre: c.Nombre,
            Descripcion: c.Descripcion,
            CreatedAt: DateTime.UtcNow.AddDays(-_random.Next(30, 365)),
            UpdatedAt: DateTime.UtcNow.AddHours(-_random.Next(0, 24))
        );
    }

    private long ExtractIdFromQuery(string query)
    {
        try
        {
            // Extraer ID de queries como producto(id: 1) o producto(1)
            var idMatch = System.Text.RegularExpressions.Regex.Match(query, @"(?:id:\s*(\d+)|(\d+)\s*\)");
            if (idMatch.Success)
            {
                var idValue = idMatch.Groups[1].Success ? idMatch.Groups[1].Value : idMatch.Groups[2].Value;
                if (long.TryParse(idValue, out var id))
                    return id;
            }

            // Si no se puede parsear, devolver ID por defecto (1)
            return 1;
        }
        catch
        {
            // En caso de error, devolver ID por defecto
            return 1;
        }
    }

    private long ExtractIdFromMutation(string mutation)
    {
        // Extraer ID de mutations como updateProducto(id: 1, ...) o deleteProducto(id: 1)
        var idMatch = System.Text.RegularExpressions.Regex.Match(mutation, @"id:\s*(\d+)");
        return idMatch.Success ? long.Parse(idMatch.Groups[1].Value) : 0;
    }

    private T? ExtractInputFromMutation<T>(string mutation, string operationName) where T : class
    {
        try
        {
            // Buscar el bloque input: { ... } en la mutation
            var inputMatch = System.Text.RegularExpressions.Regex.Match(
                mutation,
                $@"{operationName}\s*\(\s*[^)]*input:\s*\{{\s*(.*?)\s*\}}\s*\)",
                System.Text.RegularExpressions.RegexOptions.Singleline
            );

            if (!inputMatch.Success)
                return null;

            var inputJson = "{" + inputMatch.Groups[1].Value + "}";
            return JsonSerializer.Deserialize<T>(inputJson);
        }
        catch
        {
            return null;
        }
    }
}



internal class GraphQLProductoSimulado(long id, string nombre, string descripcion, decimal precio, string? imagenUrl, long? categoriaId = null)
{
    public long Id { get; } = id;
    public string Nombre { get; set; } = nombre;
    public string Descripcion { get; set; } = descripcion;
    public decimal Precio { get; set; } = precio;
    public string? ImagenUrl { get; set; } = imagenUrl;
    public long? CategoriaId { get; set; } = categoriaId;
}

internal class GraphQLCategoriaSimulada(long id, string nombre, string descripcion)
{
    public long Id { get; } = id;
    public string Nombre { get; } = nombre;
    public string Descripcion { get; } = descripcion;
    public DateTime CreatedAt { get; } = DateTime.UtcNow.AddDays(-30);
    public DateTime UpdatedAt { get; } = DateTime.UtcNow.AddHours(-1);
}