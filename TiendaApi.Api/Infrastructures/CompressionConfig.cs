using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de compresión HTTP (Brotli + Gzip).
/// </summary>
public static class CompressionConfig
{
    /// <summary>
    /// Registra los proveedores de compresión HTTP.
    /// </summary>
    public static IServiceCollection AddResponseCompressionConfig(this IServiceCollection services)
    {
        Log.Information("📦 Configurando compresión HTTP (Brotli + Gzip)...");

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                ["application/json", "application/xml", "text/plain", "text/html", "text/css", "application/javascript"]);
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }
}
