using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Data.Seed.Sql;

/// <summary>
/// Seeder para datos iniciales de PostgreSQL.
/// Crea usuarios, categorías y productos de ejemplo.
/// </summary>
public class SqlSeeder
{
    private readonly TiendaDbContext _context;
    private readonly ILogger<SqlSeeder> _logger;

    public SqlSeeder(TiendaDbContext context, ILogger<SqlSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Sembrar datos iniciales si no existen.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Sembrando datos iniciales en PostgreSQL...");

            await SeedUsersAsync();
            await SeedCategoriasAsync();
            await SeedProductosAsync();

            _logger.LogInformation("Datos de PostgreSQL sembrados correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al sembrar datos en PostgreSQL");
        }
    }

    private async Task SeedUsersAsync()
    {
        // Contraseñas en texto plano (para testing):
        // Admin: admin@tienda.com / admin
        // User:  userdaw@tienda.com / userdaw
        var users = new List<User>
        {
            new()
            {
                Username = "admin",
                Email = "admin@tienda.com",
                PasswordHash = "$2a$11$oET21qjcdCsP/5xzDij2QOVHnHF04Ipc5HervdO45pkKnE8lpdCrK",
                Role = UserRoles.ADMIN,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "userdaw",
                Email = "userdaw@tienda.com",
                PasswordHash = "$2a$11$0NFi7iTLKAmHXF5lq18h..zp/KtjzsNUXUsAlpdVtTTG4dpf0nyBe",
                Role = UserRoles.USER,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Insertados {Count} usuarios de ejemplo", users.Count);
    }

    private async Task SeedCategoriasAsync()
    {
        var categorias = new List<Categoria>
        {
            new()
            {
                Nombre = "Electrónica",
                Descripcion = "Dispositivos electrónicos y gadgets",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Nombre = "Ropa",
                Descripcion = "Prendas de vestir para todas las edades",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Nombre = "Libros",
                Descripcion = "Libros de todos los géneros y temáticas",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Categorias.AddRangeAsync(categorias);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Insertadas {Count} categorías de ejemplo", categorias.Count);
    }

    private async Task SeedProductosAsync()
    {
        var productos = new List<Producto>
        {
            new()
            {
                Nombre = "Laptop Dell XPS 15",
                Descripcion = "Laptop de alto rendimiento",
                Precio = 1299.99m,
                Stock = 10,
                CategoriaId = 1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            },
            new()
            {
                Nombre = "Camiseta Nike",
                Descripcion = "Camiseta deportiva",
                Precio = 29.99m,
                Stock = 50,
                CategoriaId = 2,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = new byte[] { 2, 3, 4, 5, 6, 7, 8, 9 }
            },
            new()
            {
                Nombre = "Clean Code",
                Descripcion = "Libro de Robert C. Martin",
                Precio = 42.99m,
                Stock = 25,
                CategoriaId = 3,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = new byte[] { 3, 4, 5, 6, 7, 8, 9, 10 }
            }
        };

        await _context.Productos.AddRangeAsync(productos);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Insertados {Count} productos de ejemplo", productos.Count);
    }
}
