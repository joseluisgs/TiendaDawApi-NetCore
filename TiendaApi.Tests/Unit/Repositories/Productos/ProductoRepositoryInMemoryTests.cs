using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Repositories.Productos;

public class ProductoRepositoryInMemoryTests
{
    private TiendaDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TiendaDbContext(options);
    }

    private static byte[] NewRowVersion() => new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

    [Test]
    public async Task ExistsAsync_Existe_RetornaTrue()
    {
        using var context = CreateContext(nameof(ExistsAsync_Existe_RetornaTrue));

        context.Productos.Add(new Producto { Id = 1, Nombre = "Laptop", RowVersion = NewRowVersion() });
        await context.SaveChangesAsync();

        var result = await context.Productos.AnyAsync(p => p.Id == 1);

        result.Should().BeTrue();
    }

    [Test]
    public async Task ExistsAsync_NoExiste_RetornaFalse()
    {
        using var context = CreateContext(nameof(ExistsAsync_NoExiste_RetornaFalse));

        var result = await context.Productos.AnyAsync(p => p.Id == 999);

        result.Should().BeFalse();
    }

    [Test]
    public async Task SaveAsync_NuevoProducto_RetornaConId()
    {
        using var context = CreateContext(nameof(SaveAsync_NuevoProducto_RetornaConId));

        var producto = new Producto { Id = 100, Nombre = "Tablet", Precio = 299.99m, Stock = 20, CategoriaId = 1, RowVersion = NewRowVersion() };

        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        producto.Id.Should().Be(100);
        producto.Nombre.Should().Be("Tablet");
    }

    [Test]
    public async Task UpdateAsync_Existente_ActualizaPrecioYStock()
    {
        using var context = CreateContext(nameof(UpdateAsync_Existente_ActualizaPrecioYStock));

        context.Productos.Add(new Producto { Id = 1, Nombre = "Laptop", Precio = 999.99m, Stock = 10, RowVersion = NewRowVersion() });
        await context.SaveChangesAsync();

        var producto = await context.Productos.FindAsync(1L);
        producto!.Precio = 899.99m;
        producto.Stock = 5;
        producto.RowVersion = NewRowVersion();

        context.Productos.Update(producto);
        await context.SaveChangesAsync();

        producto.Precio.Should().Be(899.99m);
        producto.Stock.Should().Be(5);
    }

    [Test]
    public async Task DeleteAsync_Existente_MarcaIsDeleted()
    {
        using var context = CreateContext(nameof(DeleteAsync_Existente_MarcaIsDeleted));

        context.Productos.Add(new Producto { Id = 1, Nombre = "Para Borrar", RowVersion = NewRowVersion() });
        await context.SaveChangesAsync();

        var producto = await context.Productos.FindAsync(1L);
        producto!.IsDeleted = true;

        context.Productos.Update(producto);
        await context.SaveChangesAsync();

        var resultado = await context.Productos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == 1);
        resultado.Should().NotBeNull();
        resultado!.IsDeleted.Should().BeTrue();
    }

    #region FindAllPagedAsync Tests

    /// <summary>
    /// Verifica que la paginación retorna el número correcto de elementos.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_Con20Elementos_Pagina1_Size10_Retorna10()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        for (int i = 1; i <= 20; i++)
        {
            context.Productos.Add(new Producto
            {
                Id = i,
                Nombre = $"Producto {i}",
                Precio = 100m + i,
                Stock = 10,
                CategoriaId = 1,
                IsDeleted = false,
                RowVersion = NewRowVersion()
            });
        }
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().HaveCount(10);
        totalCount.Should().Be(20);
    }

    /// <summary>
    /// Verifica que la paginación retorna la página correcta.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_Pagina2_Size5_RetornaElementos11a15()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        for (int i = 1; i <= 20; i++)
        {
            context.Productos.Add(new Producto
            {
                Id = i,
                Nombre = $"Producto {i}",
                Precio = 100m,
                Stock = 10,
                CategoriaId = 1,
                IsDeleted = false,
                RowVersion = NewRowVersion()
            });
        }
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto(null, null, null, null, null, 1, 5, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().HaveCount(5);
        items.First().Id.Should().Be(6);
        items.Last().Id.Should().Be(10);
    }

    /// <summary>
    /// Verifica que el filtro por nombre funciona correctamente.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_ConFiltroNombre_RetornaSoloCoincidentes()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Laptop Gaming", Precio = 1000m, Stock = 10, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Laptop Oficina", Precio = 800m, Stock = 15, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Mouse Inalambrico", Precio = 50m, Stock = 100, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto("Laptop", null, null, null, null, 0, 10, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    /// <summary>
    /// Verifica que el filtro por precio máximo funciona correctamente.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_ConFiltroPrecioMax_RetornaProductosBaratos()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Producto Caro", Precio = 1000m, Stock = 10, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Producto Mediano", Precio = 500m, Stock = 20, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Producto Barato", Precio = 100m, Stock = 50, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto(null, null, null, 600m, null, 0, 10, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().HaveCount(2);
        items.All(p => p.Precio <= 600m).Should().BeTrue();
    }

    /// <summary>
    /// Verifica que el filtro por stock mínimo funciona correctamente.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_ConFiltroStockMin_RetornaProductosConStock()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Poco Stock", Precio = 100m, Stock = 5, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Stock Medio", Precio = 200m, Stock = 25, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Mucho Stock", Precio = 300m, Stock = 100, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto(null, null, null, null, 20, 0, 10, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().HaveCount(2);
        items.All(p => p.Stock >= 20).Should().BeTrue();
    }

    /// <summary>
    /// Verifica que el filtro IsDeleted funciona correctamente.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_ConFiltroIsDeleted_RetornaSoloEliminados()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Activo", Precio = 100m, Stock = 10, IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Eliminado", Precio = 200m, Stock = 20, IsDeleted = true, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Otro Eliminado", Precio = 300m, Stock = 30, IsDeleted = true, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.IgnoreQueryFilters().AsQueryable();
        var filter = new ProductoFilterDto(null, null, true, null, null, 0, 10, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().HaveCount(2);
        items.All(p => p.IsDeleted).Should().BeTrue();
    }

    /// <summary>
    /// Verifica que la ordenación descendente funciona correctamente.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_OrdenacionDescendente_RetornaOrdenInverso()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Primero", Precio = 100m, Stock = 10, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Segundo", Precio = 200m, Stock = 20, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Tercero", Precio = 300m, Stock = 30, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "id", "desc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.First().Id.Should().Be(3);
        items.Last().Id.Should().Be(1);
    }

    /// <summary>
    /// Verifica que la ordenación por precio funciona correctamente.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_OrdenacionPorPrecio_RetornaOrdenadoPorPrecio()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Caro", Precio = 300m, Stock = 10, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Barato", Precio = 100m, Stock = 20, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Medio", Precio = 200m, Stock = 30, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "precio", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.First().Precio.Should().Be(100m);
        items.Skip(1).First().Precio.Should().Be(200m);
        items.Last().Precio.Should().Be(300m);
    }

    /// <summary>
    /// Verifica que página vacía retorna cero elementos.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_PaginaVacia_RetornaCeroElementos()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Producto 1", Precio = 100m, Stock = 10, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Producto 2", Precio = 200m, Stock = 20, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto(null, null, null, null, null, 10, 10, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().BeEmpty();
        totalCount.Should().Be(2);
    }

    /// <summary>
    /// Verifica que el TotalCount es correcto con filtros combinados.
    /// </summary>
    [Test]
    public async Task FindAllPagedAsync_FiltrosCombinados_TotalCountEsCorrecto()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Laptop Gaming", Precio = 800m, Stock = 10, IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Laptop Oficina", Precio = 800m, Stock = 15, IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Tablet Pro", Precio = 600m, Stock = 20, IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 4, Nombre = "Laptop Vieja", Precio = 400m, Stock = 5, IsDeleted = true, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var query = context.Productos.AsQueryable();
        var filter = new ProductoFilterDto("Laptop", null, false, 900m, null, 0, 10, "id", "asc");

        var (items, totalCount) = await GetPagedResult(query, filter);

        items.Should().HaveCount(2);
        items.All(p => p.Nombre.Contains("Laptop") && !p.IsDeleted && p.Precio <= 900m).Should().BeTrue();
        totalCount.Should().Be(2);
    }

    #endregion

    /// <summary>
    /// Método auxiliar para ejecutar paginación en tests.
    /// </summary>
    private static async Task<(IEnumerable<Producto> Items, int TotalCount)> GetPagedResult(
        IQueryable<Producto> query,
        ProductoFilterDto filter,
        bool applyFilters = true)
    {
        var filteredQuery = query.AsQueryable();

        if (applyFilters)
        {
            if (!string.IsNullOrWhiteSpace(filter.Nombre))
                filteredQuery = filteredQuery.Where(p => p.Nombre.ToLower().Contains(filter.Nombre.ToLower()));

            if (filter.IsDeleted.HasValue)
                filteredQuery = filteredQuery.Where(p => p.IsDeleted == filter.IsDeleted.Value);

            if (filter.PrecioMax.HasValue)
                filteredQuery = filteredQuery.Where(p => p.Precio <= filter.PrecioMax.Value);

            if (filter.StockMin.HasValue)
                filteredQuery = filteredQuery.Where(p => p.Stock >= filter.StockMin.Value);
        }

        var totalCount = await filteredQuery.CountAsync();

        filteredQuery = ApplySorting(filteredQuery, filter.SortBy, filter.Direction);

        var items = await filteredQuery
            .Skip(filter.Page * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

        return (items, totalCount);
    }

    private static IQueryable<Producto> ApplySorting(IQueryable<Producto> query, string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        query = sortBy.ToLower() switch
        {
            "nombre" => isDescending ? query.OrderByDescending(p => p.Nombre) : query.OrderBy(p => p.Nombre),
            "precio" => isDescending ? query.OrderByDescending(p => p.Precio) : query.OrderBy(p => p.Precio),
            "stock" => isDescending ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
            "createdat" => isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => isDescending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
        };

        return query;
    }

    #region GetRecentlyCreatedAsync Tests

    /// <summary>
    /// Verifica que con productos en la base de datos retorna la cantidad correcta.
    /// </summary>
    [Test]
    public async Task GetRecentlyCreatedAsync_ConProductosEnBD_RetornaLosMismos()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_ConProductosEnBD_RetornaLosMismos));

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Producto 1", RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Producto 2", RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Producto 3", RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var productos = await context.Productos
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        productos.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifica que excluye productos eliminados lógicamente.
    /// </summary>
    [Test]
    public async Task GetRecentlyCreatedAsync_ConProductoEliminado_ExcluyeProducto()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_ConProductoEliminado_ExcluyeProducto));

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Producto Activo", IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Producto Eliminado", IsDeleted = true, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var productos = await context.Productos
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !p.IsDeleted)
            .ToListAsync();

        productos.Should().HaveCount(1);
        productos.First().Nombre.Should().Be("Producto Activo");
    }

    /// <summary>
    /// Verifica que productos eliminados lógicamente no son incluidos.
    /// </summary>
    [Test]
    public async Task GetRecentlyCreatedAsync_MultiplesEliminados_ExcluyeTodos()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_MultiplesEliminados_ExcluyeTodos));

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Eliminado 1", IsDeleted = true, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Eliminado 2", IsDeleted = true, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Eliminado 3", IsDeleted = true, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var productos = await context.Productos
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !p.IsDeleted)
            .ToListAsync();

        productos.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que productos activos son retornados.
    /// </summary>
    [Test]
    public async Task GetRecentlyCreatedAsync_ConProductosActivos_RetornaTodos()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_ConProductosActivos_RetornaTodos));

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Activo 1", IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Activo 2", IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Activo 3", IsDeleted = false, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var productos = await context.Productos
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !p.IsDeleted)
            .ToListAsync();

        productos.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifica que con un solo producto retorna ese producto.
    /// </summary>
    [Test]
    public async Task GetRecentlyCreatedAsync_UnProducto_RetornaEseProducto()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_UnProducto_RetornaEseProducto));

        context.Productos.Add(new Producto { Id = 1, Nombre = "Único", RowVersion = NewRowVersion() });
        await context.SaveChangesAsync();

        var productos = await context.Productos
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !p.IsDeleted)
            .ToListAsync();

        productos.Should().HaveCount(1);
        productos.First().Nombre.Should().Be("Único");
    }

    /// <summary>
    /// Verifica que sin productos retorna lista vacía.
    /// </summary>
    [Test]
    public async Task GetRecentlyCreatedAsync_SinProductos_RetornaListaVacia()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_SinProductos_RetornaListaVacia));

        var productos = await context.Productos
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !p.IsDeleted)
            .ToListAsync();

        productos.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que con productos mixtos (activos y eliminados) solo retorna activos.
    /// </summary>
    [Test]
    public async Task GetRecentlyCreatedAsync_ProductosMixtos_RetornaSoloActivos()
    {
        using var context = CreateContext(nameof(GetRecentlyCreatedAsync_ProductosMixtos_RetornaSoloActivos));

        context.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Activo 1", IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 2, Nombre = "Eliminado 1", IsDeleted = true, RowVersion = NewRowVersion() },
            new Producto { Id = 3, Nombre = "Activo 2", IsDeleted = false, RowVersion = NewRowVersion() },
            new Producto { Id = 4, Nombre = "Eliminado 2", IsDeleted = true, RowVersion = NewRowVersion() }
        );
        await context.SaveChangesAsync();

        var productos = await context.Productos
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7) && !p.IsDeleted)
            .ToListAsync();

        productos.Should().HaveCount(2);
        productos.All(p => !p.IsDeleted).Should().BeTrue();
    }

    #endregion
}
