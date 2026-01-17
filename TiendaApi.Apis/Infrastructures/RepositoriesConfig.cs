using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Repositories.Pedidos;
using TiendaApi.Apis.Repositories.Usuarios;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de repositorios.
/// </summary>
public static class RepositoriesConfig
{
    /// <summary>
    /// Registra todos los repositorios en el contenedor de dependencias.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        Log.Information("📦 Registrando repositorios...");
        return services
            .AddScoped<ICategoriaRepository, CategoriaRepository>()
            .AddScoped<IProductoRepository, ProductoRepository>()
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<IPedidosRepository, PedidosRepository>();
    }
}
