using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Services.Pedidos;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Infrastructures;

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
            .AddTransient<IJwtTokenExtractor, JwtTokenExtractor>()
            .AddScoped<IAuthService, AuthService>()
            .AddScoped<IUserService, UserService>();
    }
}
