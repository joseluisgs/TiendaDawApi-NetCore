using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Services.Background.Jobs;

/// <summary>
    /// Servicio de reportes de productos.
    /// Obtiene productos nuevos y envía notificaciones por email.
    /// </summary>
    public class ProductoReportTask(
    IProductoRepository productoRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    ILogger<ProductoReportTask> logger,
    IConfiguration configuration
) : IProductoReportTask
{
    private readonly int _days = configuration.GetValue<int>("Scheduler:ProductoReportDays", 7);
    private readonly bool _isDevelopment = configuration.GetValue<bool>("IsDevelopment");

    /// <summary>
    /// Ejecuta el reporte de productos nuevos.
    /// </summary>
    /// <returns>UnitResult con error en caso de fallo.</returns>
    public async Task<UnitResult<DomainError>> ExecuteAsync()
    {
        logger.LogInformation("Ejecutando reporte de productos - Modo: {Modo}", 
            _isDevelopment ? "DESARROLLO" : "PRODUCCION");

        if (_isDevelopment)
        {
            logger.LogInformation("Servicio de Background esta funcionando - Modo desarrollo");
            return UnitResult.Success<DomainError>();
        }

        return await GetRecentlyCreatedProductsAsync()
            .Bind(products => SendEmailsToActiveUsersAsync(products));
    }

    /// <summary>
    /// Obtiene productos creados en los últimos X días.
    /// </summary>
    private async Task<Result<IEnumerable<Producto>, DomainError>> GetRecentlyCreatedProductsAsync()
    {
        logger.LogDebug("Obteniendo productos de los ultimos {Dias} dias", _days);

        var productos = await productoRepository.GetRecentlyCreatedAsync(_days);
        
        logger.LogInformation("Encontrados {Cantidad} productos nuevos", productos.Count());
        return Result.Success<IEnumerable<Producto>, DomainError>(productos);
    }

    /// <summary>
    /// Envía emails a todos los usuarios activos con los productos nuevos.
    /// </summary>
    private async Task<UnitResult<DomainError>> SendEmailsToActiveUsersAsync(IEnumerable<Producto> productos)
    {
        if (!productos.Any())
        {
            logger.LogInformation("No hay productos nuevos para reportar");
            return UnitResult.Success<DomainError>();
        }

        logger.LogInformation("Enviando emails a usuarios activos con {Cantidad} productos", productos.Count());

        var usuarios = await userRepository.GetActiveUsersAsync();

        if (!usuarios.Any())
        {
            logger.LogInformation("No hay usuarios activos para enviar reportes");
            return UnitResult.Success<DomainError>();
        }

        foreach (var user in usuarios)
        {
            var email = new EmailMessage
            {
                To = user.Email,
                Subject = $"Novedades en Tienda: {productos.Count()} productos nuevos",
                Body = GenerateHtmlEmail(productos, user.Username),
                IsHtml = true
            };

            await emailService.EnqueueEmailAsync(email);
            logger.LogInformation("Email encolado para {Email}", user.Email);
        }

        logger.LogInformation("Emails encolados para {Cantidad} usuarios", usuarios.Count());
        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Genera el cuerpo del email en formato HTML.
    /// </summary>
    private static string GenerateHtmlEmail(IEnumerable<Producto> productos, string userName)
    {
        var productosHtml = string.Concat(productos.Select(p => string.Format(@"
            <div style=""border: 1px solid #ddd; padding: 15px; margin: 10px 0; border-radius: 8px;"">
                <h3 style=""margin: 0 0 10px 0;"">{0}</h3>
                <p style=""margin: 0; color: #666;"">{1}</p>
                <p style=""margin: 10px 0 0 0; font-weight: bold; color: #28a745;"">
                    {2} - Stock: {3}
                </p>
            </div>", p.Nombre, p.Descripcion, p.Precio.ToString("C"), p.Stock)));

        return string.Format(@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 8px 8px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Novedades de la Semana</h1>
        </div>
        <div class=""content"">
            <p>Hola <strong>{0}</strong></p>
            <p>Te presentamos los <strong>{1}</strong> productos anadidos esta semana:</p>
            {2}
        </div>
        <div class=""footer"">
            <p>TiendaDAW - Tu tienda de confianza</p>
        </div>
    </div>
</body>
</html>", userName, productos.Count(), productosHtml);
    }
}
