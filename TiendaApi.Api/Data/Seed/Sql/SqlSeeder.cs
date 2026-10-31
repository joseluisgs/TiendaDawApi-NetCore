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
/// - "Custom" (default): hash BCrypt calculado de <see cref="SeedCredentials"/>
/// - "Identity": UserManager (PBKDF2) con las MISMAS credenciales
/// El modo solo cambia el algoritmo del hash: mismo usuario y contraseña en ambos.
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

            // ⚠️ Credenciales unificadas (SeedCredentials): las MISMAS que en Custom.
            // La política de Identity (≥8, mayúscula y dígito) las acepta, así que el
            // modo solo determina el algoritmo del hash, nunca quién puede entrar.
            // El modelo User no tiene campos de perfil (username/email/rol/credencial):
            // la semilla solo fija esos.
            var identityUsers = new List<(User User, string Password)>
            {
                (new() { UserName = SeedCredentials.AdminUsername, Email = "admin@tienda.com", Role = UserRoles.ADMIN },
                    SeedCredentials.AdminPassword),
                (new() { UserName = SeedCredentials.UserUsername, Email = "userdaw@tienda.com", Role = UserRoles.USER },
                    SeedCredentials.UserPassword)
            };

            foreach (var (user, password) in identityUsers)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (user.Role == UserRoles.ADMIN)
                        await userManager.AddToRoleAsync(user, UserRoles.ADMIN);
                    _logger.LogInformation("Usuario Identity creado: {Email} (contraseña seed: {Password})",
                        user.Email, password);
                }
                else
                {
                    _logger.LogWarning("Error creando usuario {Email}: {Errors}",
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            return;
        }

        // Modo Custom (default): hash BCrypt calculado en el momento desde
        // SeedCredentials — la MISMA contraseña que en Identity, solo cambia
        // el algoritmo del hash. Sin hashes hardcoded que puedan divergir.
        var users = new List<User>
        {
            new()
            {
                UserName = SeedCredentials.AdminUsername,
                Email = "admin@tienda.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(SeedCredentials.AdminPassword, workFactor: 11),
                Role = UserRoles.ADMIN,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                UserName = SeedCredentials.UserUsername,
                Email = "userdaw@tienda.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(SeedCredentials.UserPassword, workFactor: 11),
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
