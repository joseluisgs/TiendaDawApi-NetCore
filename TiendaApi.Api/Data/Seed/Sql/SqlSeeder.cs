using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Data.Seed.Sql;

/// <summary>
/// Seeder para datos iniciales de PostgreSQL.
/// Crea usuarios, categorías y productos de ejemplo.
///
/// Los usuarios se crean condicionalmente según Auth:Mode:
/// - "Custom" (default): hashes BCrypt hardcoded → admin@tienda.com / admin
/// - "Identity": UserManager (PBKDF2) → admin@tienda.com / Admin123!
///   (la política de password de Identity exige longitud ≥6, dígito y mayúscula)
/// </summary>
public class SqlSeeder
{
    private readonly TiendaDbContext _context;
    private readonly ILogger<SqlSeeder> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Crea una instancia del seeder de PostgreSQL.
    /// </summary>
    public SqlSeeder(TiendaDbContext context, ILogger<SqlSeeder> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
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
        var authMode = _configuration["Auth:Mode"] ?? "Custom";

        if (authMode == "Identity")
        {
            // 🎓 Modo Identity: usar UserManager para generar hashes PBKDF2 compatibles.
            // Los hashes BCrypt hardcoded NO funcionan con Identity (usa PBKDF2).
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

            // AddToRoleAsync lanza excepción si el rol no existe → crearlos antes.
            foreach (var role in new[] { UserRoles.ADMIN, UserRoles.USER })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<long>(role));
                    _logger.LogInformation("Rol Identity creado: {Role}", role);
                }
            }

            // ⚠️ La política de password de Identity (AuthenticationConfig) exige
            // longitud ≥6, dígito y mayúscula. "admin" NO cumple → usar "Admin123!".
            const string identityPassword = "Admin123!";

            var identityUsers = new List<User>
            {
                new() { UserName = "admin", Email = "admin@tienda.com", Role = UserRoles.ADMIN, Nombre = "Admin", Apellidos = "Admin" },
                new() { UserName = "userdaw", Email = "userdaw@tienda.com", Role = UserRoles.USER, Nombre = "User", Apellidos = "Daw" }
            };

            foreach (var user in identityUsers)
            {
                var result = await userManager.CreateAsync(user, identityPassword);
                if (result.Succeeded)
                {
                    if (user.Role == UserRoles.ADMIN)
                        await userManager.AddToRoleAsync(user, UserRoles.ADMIN);
                    _logger.LogInformation("Usuario Identity creado: {Email} (contraseña seed: {Password})",
                        user.Email, identityPassword);
                }
                else
                {
                    _logger.LogWarning("Error creando usuario {Email}: {Errors}",
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            return;
        }

        // Modo Custom (default): hashes BCrypt hardcoded
        // Contraseñas en texto plano (para testing):
        // Admin: admin@tienda.com / admin
        // User:  userdaw@tienda.com / userdaw
        var users = new List<User>
        {
            new()
            {
                UserName = "admin",
                Email = "admin@tienda.com",
                PasswordHash = "$2a$11$oET21qjcdCsP/5xzDij2QOVHnHF04Ipc5HervdO45pkKnE8lpdCrK",
                Role = UserRoles.ADMIN,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                UserName = "userdaw",
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
