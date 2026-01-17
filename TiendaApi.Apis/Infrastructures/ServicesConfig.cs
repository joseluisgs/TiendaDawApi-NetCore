using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.Services;
using TiendaApi.Apis.Services.Auth;
using TiendaApi.Apis.Services.Categorias;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Services.Productos;
using TiendaApi.Apis.Services.Users;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de servicios de negocio.
/// </summary>
public static class ServicesConfig
{
    /// <summary>
    /// Registra todos los servicios de negocio en el contenedor de dependencias.
    /// </summary>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        Log.Information("⚙️ Registrando servicios...");
        return services
            .AddScoped<ICategoriaService, CategoriaService>()
            .AddScoped<IProductoService, ProductoService>()
            .AddScoped<IPedidosService, PedidosService>()
            .AddScoped<IJwtService, JwtService>()
            .AddScoped<IJwtTokenExtractor, JwtTokenExtractor>()
            .AddScoped<IAuthService, AuthService>()
            .AddScoped<IUserService, UserService>();
    }
}
