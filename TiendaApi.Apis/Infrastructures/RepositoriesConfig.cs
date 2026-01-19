using Microsoft.Extensions.Configuration;
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
    /// 
    /// <para>
    /// El repositorio de pedidos se elige según configuration["Pedidos:RepositoryType"]:
    /// <list type="bullet">
    ///   <item><b>MongoDbNative:</b> Usa PedidosNativeRepository (driver nativo, funcional)</item>
    ///   <item><b>MongoDbEfCore:</b> Usa PedidosEfCoreRepository (Entity Framework Core, tiene bug EF-272)</item>
    /// </list>
    /// </para>
    /// </summary>
    public static IServiceCollection AddRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Log.Information("📦 Registrando repositorios...");

        // Repositorios que no dependen de MongoDB
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // ============================================
        // REPOSITORIO DE PEDIDOS (según configuración)
        // ============================================
        // La elección se basa en configuration["Pedidos:RepositoryType"]
        // que está configurado en appsettings.json
        // ============================================
        
        var pedidosRepoType = configuration["Pedidos:RepositoryType"] ?? "MongoDbNative";

        if (pedidosRepoType == "MongoDbNative")
        {
            // MongoDB Driver nativo (recomendado)
            services.AddScoped<IPedidosRepository, PedidosNativeRepository>();
            Log.Debug("📦 Usando PedidosNativeRepository (MongoDB Driver nativo)");
        }
        else
        {
            // MongoDB Entity Framework Core (tiene bug)
            services.AddScoped<IPedidosRepository, PedidosEfCoreRepository>();
            Log.Debug("📦 Usando PedidosEfCoreRepository (MongoDB EF Core)");
        }

        return services;
    }
}
