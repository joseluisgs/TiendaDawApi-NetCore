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
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("PostgreSQL ya contiene usuarios, omitiendo sembrado");
                return;
            }

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
                Id = 1,
                Username = "admin",
                Email = "admin@tienda.com",
                PasswordHash = "$2a$11$vHqmFyFyRqKtaVJEz0XqFeI/xlPNGOKJbBYGzN0PqnQZQqZm3LzYy", // bcrypt("admin")
                Role = UserRoles.ADMIN,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Username = "userdaw",
                Email = "userdaw@tienda.com",
                PasswordHash = "$2a$11$y6x2PMrc.RgbGfXM.UVMReFNNQs6YnmsdAm2S3ieRo/FlWb86gLsi", // bcrypt("userdaw")
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
                Id = 1,
                Nombre = "Electrónica",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Nombre = "Ropa",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Nombre = "Libros",
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
                Id = 1,
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
                Id = 2,
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
                Id = 3,
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
