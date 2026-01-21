using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de repositorios.
/// </summary>
public static class RepositoriesConfig
{
    /// <summary>
    /// Registra todos los repositorios.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Registrando repositorios...");

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        var pedidosRepoType = configuration["Pedidos:RepositoryType"] ?? "MongoDbNative";

        if (pedidosRepoType == "MongoDbNative")
        {
            services.AddScoped<IPedidosRepository, PedidosNativeRepository>();
            Log.Debug("Usando PedidosNativeRepository (MongoDB Driver nativo)");
        }
        else
        {
            services.AddScoped<IPedidosRepository, PedidosEfCoreRepository>();
            Log.Debug("Usando PedidosEfCoreRepository (MongoDB EF Core)");
        }

        return services;
    }
}
