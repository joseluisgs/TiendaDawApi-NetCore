using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Services.Historial;
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
            .AddScoped<IProductoEffects, ProductoEffects>()
            .AddScoped<IHistorialService, HistorialService>()
            .AddScoped<IPedidosService, PedidosService>()
            .AddScoped<IPedidoEffects, PedidoEffects>()
            .AddScoped<IJwtService, JwtService>()
            .AddTransient<IJwtTokenExtractor, JwtTokenExtractor>()
            // IAuthService NO se registra aquí: lo decide AuthenticationConfig según Auth:Mode
            // (Custom → AuthService / Identity → IdentityAuthService). Registrarlo aquí lo
            // sobreescribía y anulaba el switch (en DI gana el último registro).
            .AddScoped<IUserService, UserService>();
    }
}
