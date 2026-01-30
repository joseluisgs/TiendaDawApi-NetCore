using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.State;
using ClientBlazor.Cliente.DTOs.Common;
using ClientBlazor.Cliente.DTOs.Productos;
using ClientBlazor.Cliente.DTOs.Categorias;
using CSharpFunctionalExtensions;
using System;
using static CSharpFunctionalExtensions.Result;

namespace ClientBlazor.Cliente.Services;

/// <summary>
/// Representa un valor vacío para Results que no devuelven datos.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = default;
}

/// <summary>
/// Servicio REST simulado - simula operaciones REST sin conectar realmente.
/// Usa Railway Oriented Programming con Result<T,E>.
/// Requiere autenticación para operaciones POST/PUT/DELETE.
/// </summary>
public class RestService(
    /// <summary>
    /// Store de autenticación para validar tokens.
    /// </summary>
    AuthStore authStore)
{
    private readonly Random _random = new();

    /// <summary>
    /// Valida que el usuario esté autenticado antes de operaciones que requieren token.
    /// Operaciones POST, PUT, DELETE requieren autenticación válida.
    /// </summary>
    /// <returns>Resultado de validación de autenticación.</returns>
    private Result<Unit, DomainError> ValidateAuthentication()
    {
        if (!authStore.GetState().IsAuthenticated)
            return Result.Failure<Unit, DomainError>(AuthErrors.LoginRequired);

        if (string.IsNullOrEmpty(authStore.GetState().Token))
            return Result.Failure<Unit, DomainError>(AuthErrors.TokenExpired);

        return Result.Success<Unit, DomainError>(Unit.Value);
    }

    // Datos simulados en memoria
    private readonly List<ProductoSimulado> _productos = new()
    {
        new ProductoSimulado(1, "iPhone 15", "Teléfono móvil Apple", 999.99m, "iphone.jpg"),
        new ProductoSimulado(2, "Samsung Galaxy S24", "Teléfono móvil Samsung", 899.99m, "galaxy.jpg"),
        new ProductoSimulado(3, "MacBook Pro", "Ordenador portátil Apple", 1999.99m, "macbook.jpg"),
        new ProductoSimulado(4, "Dell XPS 13", "Ordenador portátil Dell", 1299.99m, "dell.jpg"),
        new ProductoSimulado(5, "Sony WH-1000XM5", "Auriculares inalámbricos", 349.99m, "sony.jpg")
    };

    private readonly List<CategoriaSimulada> _categorias = new()
    {
        new CategoriaSimulada(1, "Electrónica", "Productos electrónicos"),
        new CategoriaSimulada(2, "Informática", "Ordenadores y accesorios"),
        new CategoriaSimulada(3, "Teléfonos", "Móviles y accesorios")
    };

    /// <summary>
    /// Obtiene todos los productos con paginación.
    /// Simula llamada a API REST GET /api/productos con parámetros de consulta.
    /// Aplica filtros, ordenamiento y paginación a los datos simulados.
    /// </summary>
    /// <param name="filter">Filtros y opciones de paginación para la consulta.</param>
    /// <returns>Resultado con lista paginada de productos o error de dominio.</returns>
    public async Task<Result<PagedResult<ProductoDto>, DomainError>> GetProductosAsync(ProductoFilterDto filter)
    {
        try
        {
            await Task.Delay(_random.Next(200, 800)); // Simular latencia

            // Simular error aleatorio (1% de probabilidad para GET)
            if (_random.Next(100) < 1)
                return NetworkErrors.ServerError;

            var query = _productos.AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(filter.Nombre))
                query = query.Where(p => p.Nombre.Contains(filter.Nombre, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(filter.Categoria))
                query = query.Where(p => _categorias.Any(c => c.Id == p.CategoriaId && c.Nombre.Contains(filter.Categoria, StringComparison.OrdinalIgnoreCase)));

            if (filter.PrecioMax.HasValue)
                query = query.Where(p => p.Precio <= filter.PrecioMax.Value);

            // Paginación
            var totalCount = query.Count();
            var items = query
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .Select(p => new ProductoDto(
                    Id: p.Id,
                    Nombre: p.Nombre,
                    Descripcion: p.Descripcion,
                    Precio: p.Precio,
                    Stock: _random.Next(0, 100),
                    Imagen: p.ImagenUrl,
                    CategoriaId: p.CategoriaId ?? 0,
                    CategoriaNombre: GetCategoriaNombre(p.CategoriaId),
                    CreatedAt: DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
                    UpdatedAt: DateTime.UtcNow.AddHours(-_random.Next(0, 24))
                ))
                .ToList();

            var result = new PagedResult<ProductoDto>(
                Items: items,
                TotalCount: totalCount,
                Page: filter.Page,
                PageSize: filter.Size
            );

            return Result.Success<PagedResult<ProductoDto>, DomainError>(result);
        }
        catch (Exception)
        {
            return Result.Failure<PagedResult<ProductoDto>, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }

    /// <summary>
    /// Crea un nuevo producto en el sistema.
    /// Simula llamada a API REST POST /api/productos.
    /// Requiere autenticación válida. Valida los datos y crea un nuevo producto con ID generado.
    /// </summary>
    /// <param name="request">Datos del producto a crear.</param>
    /// <returns>Resultado con el producto creado o error de validación/autenticación.</returns>
    public async Task<Result<ProductoDto, DomainError>> CreateProductoAsync(ProductoRequestDto request)
    {
        // Validar autenticación primero
        var authResult = ValidateAuthentication();
        if (authResult.IsFailure)
            return Result.Failure<ProductoDto, DomainError>(authResult.Error);

        try
        {
            await Task.Delay(_random.Next(300, 800));

            // Validar datos
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return ValidationErrors.EmptyField("nombre");

            if (request.Precio <= 0)
                return ValidationErrors.InvalidEmail; // Reutilizamos error

            // Simular error aleatorio
            if (_random.Next(100) < 15)
                return AuthErrors.InsufficientPermissions; // Simular falta de permisos

            var newId = _productos.Max(p => p.Id) + 1;
            var producto = new ProductoSimulado(
                newId,
                request.Nombre,
                request.Descripcion,
                request.Precio,
                request.Imagen,
                request.CategoriaId
            );

            _productos.Add(producto);

            var dto = new ProductoDto(
                Id: producto.Id,
                Nombre: producto.Nombre,
                Descripcion: producto.Descripcion,
                Precio: producto.Precio,
                Stock: _random.Next(10, 50),
                Imagen: producto.ImagenUrl,
                CategoriaId: producto.CategoriaId ?? 0,
                CategoriaNombre: GetCategoriaNombre(producto.CategoriaId),
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow
            );

            return Result.Success<ProductoDto, DomainError>(dto);
        }
        catch (Exception)
        {
            return Result.Failure<ProductoDto, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }

    /// <summary>
    /// Actualiza un producto existente.
    /// Simula llamada a API REST PUT /api/productos/{id}.
    /// Requiere autenticación válida. Busca el producto, valida existencia y actualiza sus datos.
    /// </summary>
    /// <param name="id">ID del producto a actualizar.</param>
    /// <param name="request">Nuevos datos del producto.</param>
    /// <returns>Resultado con el producto actualizado o error de autenticación/validación.</returns>
    public async Task<Result<ProductoDto, DomainError>> UpdateProductoAsync(long id, ProductoRequestDto request)
    {
        // Validar autenticación primero
        var authResult = ValidateAuthentication();
        if (authResult.IsFailure)
            return Result.Failure<ProductoDto, DomainError>(authResult.Error);

        try
        {
            await Task.Delay(_random.Next(100, 300));

            // Simular error aleatorio
            if (_random.Next(100) < 5)
                return NetworkErrors.ServerError;

            var producto = _productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
                return NetworkErrors.NotFound;

            var dto = new ProductoDto(
                Id: producto.Id,
                Nombre: producto.Nombre,
                Descripcion: producto.Descripcion,
                Precio: producto.Precio,
                Stock: _random.Next(0, 100),
                Imagen: producto.ImagenUrl,
                CategoriaId: producto.CategoriaId ?? 0,
                CategoriaNombre: GetCategoriaNombre(producto.CategoriaId),
                CreatedAt: DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
                UpdatedAt: DateTime.UtcNow.AddHours(-_random.Next(0, 24))
            );

            return Result.Success<ProductoDto, DomainError>(dto);
        }
        catch (Exception)
        {
            return Result.Failure<ProductoDto, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }

    /// <summary>
    /// Elimina un producto del sistema.
    /// Simula llamada a API REST DELETE /api/productos/{id}.
    /// Requiere autenticación válida. Busca el producto y lo elimina si existe.
    /// </summary>
    /// <param name="id">ID del producto a eliminar.</param>
    /// <returns>Resultado de la eliminación o error de autenticación/validación.</returns>
    public async Task<Result<bool, DomainError>> DeleteProductoAsync(long id)
    {
        // Validar autenticación primero
        var authResult = ValidateAuthentication();
        if (authResult.IsFailure)
            return Result.Failure<bool, DomainError>(authResult.Error);

        try
        {
            await Task.Delay(_random.Next(100, 300));

            // Simular error aleatorio
            if (_random.Next(100) < 5)
                return NetworkErrors.ServerError;

            var producto = _productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
                return NetworkErrors.NotFound;

            _productos.Remove(producto);
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception)
        {
            return Result.Failure<bool, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }


    /// <summary>
    /// Obtiene todas las categorías.
    /// </summary>
    public async Task<Result<PagedResult<CategoriaDto>, DomainError>> GetCategoriasAsync(CategoriaFilterDto filter)
    {
        try
        {
            await Task.Delay(_random.Next(150, 400));

            // Simular error aleatorio
            if (_random.Next(100) < 5)
                return NetworkErrors.ServerError;

            var query = _categorias.AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(filter.Nombre))
                query = query.Where(c => c.Nombre.Contains(filter.Nombre, StringComparison.OrdinalIgnoreCase));

            // Paginación
            var totalCount = query.Count();
            var items = query
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .Select(c => new CategoriaDto(
                    Id: c.Id,
                    Nombre: c.Nombre,
                    CreatedAt: DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
                    UpdatedAt: DateTime.UtcNow.AddHours(-_random.Next(0, 24))
                ))
                .ToList();

            var result = new PagedResult<CategoriaDto>(
                Items: items,
                TotalCount: totalCount,
                Page: filter.Page,
                PageSize: filter.Size
            );

            return Result.Success<PagedResult<CategoriaDto>, DomainError>(result);
        }
        catch (Exception)
        {
            return Result.Failure<PagedResult<CategoriaDto>, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }

    /// <summary>
    /// Información del usuario autenticado.
    /// </summary>
    public record UserInfo(string Email, string Nombre, string Role, string Token)
    {
        public string DisplayName => string.IsNullOrEmpty(Nombre) ? Email.Split('@')[0] : Nombre;
    }

    /// <summary>
    /// Obtiene el nombre de la categoría por su ID.
    /// Busca la categoría en los datos simulados y devuelve su nombre.
    /// </summary>
    /// <param name="categoriaId">ID de la categoría a buscar.</param>
    /// <returns>Nombre de la categoría o "Sin categoría" si no se encuentra.</returns>
    private string GetCategoriaNombre(long? categoriaId)
    {
        if (!categoriaId.HasValue) return "Sin categoría";
        var categoria = _categorias.FirstOrDefault(c => c.Id == categoriaId.Value);
        return categoria?.Nombre ?? "Sin categoría";
    }
}

internal class ProductoSimulado(long id, string nombre, string descripcion, decimal precio, string? imagenUrl, long? categoriaId = null)
{
    public long Id { get; } = id;
    public string Nombre { get; set; } = nombre;
    public string Descripcion { get; set; } = descripcion;
    public decimal Precio { get; set; } = precio;
    public string? ImagenUrl { get; set; } = imagenUrl;
    public long? CategoriaId { get; set; } = categoriaId;
}

internal class CategoriaSimulada(long id, string nombre, string descripcion)
{
    public long Id { get; } = id;
    public string Nombre { get; } = nombre;
    public string Descripcion { get; } = descripcion;
}