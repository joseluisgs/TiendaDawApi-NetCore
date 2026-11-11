using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Primitives;
using TiendaApi.Api.Infrastructures;

namespace TiendaApi.Api.Middleware;

/// <summary>
///     Estado del rate limiting: un diccionario de particiones (IP + verbo + ruta),
///     cada una con sus propios limitadores nativos de ventana fija.
///     El estado es singleton para que los contadores sobrevivan a las peticiones.
/// </summary>
public sealed class RateLimitingState : IDisposable {
    private const int MaxPartitionsBeforeSweep = 1000;
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan IdleEvictionTime = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, RateLimitPartition> _partitions = new();
    private DateTime _lastSweepUtc = DateTime.MinValue;

    /// <summary>
    ///     Libera los limitadores (y sus temporizadores) al detener la aplicación.
    /// </summary>
    public void Dispose() {
        foreach (KeyValuePair<string, RateLimitPartition> kvp in _partitions) {
            if (_partitions.TryRemove(kvp.Key, out RateLimitPartition? removed)) {
                removed.Dispose();
            }
        }
    }

    /// <summary>
    ///     Obtiene (o crea) la partición para el cliente, verbo y ruta indicados.
    /// </summary>
    internal RateLimitPartition GetOrCreate(string clientIp, string method, string path) {
        string key = string.Create(CultureInfo.InvariantCulture, $"{clientIp}_{method}_{path}");
        RateLimitPartition partition = _partitions.GetOrAdd(key, _ => RateLimitPartition.Create(method, path));
        partition.Touch();
        SweepIfNeeded(DateTime.UtcNow);
        return partition;
    }

    private void SweepIfNeeded(DateTime now) {
        if (_partitions.Count < MaxPartitionsBeforeSweep || now - _lastSweepUtc < SweepInterval) {
            return;
        }

        _lastSweepUtc = now;
        DateTime cutoff = now - IdleEvictionTime;
        foreach (KeyValuePair<string, RateLimitPartition> kvp in _partitions) {
            if (kvp.Value.LastAccessUtc < cutoff && _partitions.TryRemove(kvp.Key, out RateLimitPartition? removed)) {
                removed.Dispose();
            }
        }
    }
}

/// <summary>
///     Contenedor de los limitadores de una partición (IP + verbo + ruta).
///     La ventana general (100/15s) siempre existe; la ventana de un minuto
///     solo se crea si la ruta es de autenticación o la petición es POST.
/// </summary>
internal sealed class RateLimitPartition : IDisposable {
    private RateLimitPartition(FixedWindowRateLimiter general, FixedWindowRateLimiter? minute, int minuteLimit) {
        General = general;
        Minute = minute;
        MinuteLimit = minuteLimit;
        CreatedUtc = DateTime.UtcNow;
        LastAccessUtc = CreatedUtc;
    }

    /// <summary>Limitador de la ventana general (100 por 15 segundos).</summary>
    internal FixedWindowRateLimiter General { get; }

    /// <summary>Limitador de la ventana de un minuto, si aplica.</summary>
    internal FixedWindowRateLimiter? Minute { get; }

    /// <summary>Límite de la ventana de un minuto (10 autenticación, 20 escritura).</summary>
    internal int MinuteLimit { get; }

    /// <summary>Indica si esta partición tiene ventana de un minuto.</summary>
    internal bool HasMinuteWindow => Minute is not null;

    /// <summary>Momento de creación, usado para calcular el reset de la ventana.</summary>
    internal DateTime CreatedUtc { get; }

    /// <summary>Último acceso, usado para la limpieza de particiones inactivas.</summary>
    internal DateTime LastAccessUtc { get; private set; }

    /// <summary>Libera los limitadores de la partición.</summary>
    public void Dispose() {
        General.Dispose();
        Minute?.Dispose();
    }

    /// <summary>Crea la partición con las reglas que correspondan a la ruta y el verbo.</summary>
    internal static RateLimitPartition Create(string method, string path) {
        var general = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions {
            PermitLimit = RateLimitConfig.GeneralLimit,
            Window = RateLimitConfig.GeneralWindow,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

        // Regla más restrictiva por periodo: autenticación (10/min) gana sobre POST (20/min).
        int minuteLimit = 0;
        if (path.StartsWith(RateLimitConfig.AuthPathPrefix, StringComparison.Ordinal)) {
            minuteLimit = RateLimitConfig.AuthLimit;
        } else if (HttpMethods.IsPost(method)) {
            minuteLimit = RateLimitConfig.WriteLimit;
        }

        FixedWindowRateLimiter? minute = null;
        if (minuteLimit > 0) {
            minute = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions {
                PermitLimit = minuteLimit,
                Window = RateLimitConfig.MinuteWindow,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
        }

        return new RateLimitPartition(general, minute, minuteLimit);
    }

    internal void Touch() {
        LastAccessUtc = DateTime.UtcNow;
    }
}

/// <summary>
///     Middleware de rate limiting sobre la API nativa <c>System.Threading.RateLimiting</c>.
///     Aplica las reglas por partición (IP + verbo + ruta), emite las cabeceras
///     estándar <c>RateLimit-Limit</c>/<c>RateLimit-Remaining</c>/<c>RateLimit-Reset</c>
///     en las respuestas permitidas y devuelve un 429 con cuerpo JSON y <c>Retry-After</c>.
/// </summary>
public class RateLimitMiddleware {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Source-gen (AppJsonContext) + fallback a reflexión para tipos anónimos
        TypeInfoResolver = JsonTypeInfoResolver.Combine(AppJsonContext.Default, new DefaultJsonTypeInfoResolver())
    };

    private readonly RequestDelegate _next;
    private readonly RateLimitingState _state;

    /// <summary>
    ///     Crea el middleware.
    /// </summary>
    /// <param name="next">Siguiente componente de la pipeline.</param>
    /// <param name="state">Estado compartido de particiones y limitadores.</param>
    public RateLimitMiddleware(RequestDelegate next, RateLimitingState state) {
        _next = next;
        _state = state;
    }

    /// <summary>
    ///     Ejecuta la comprobación de límites para la petición en curso.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición.</param>
    public async Task InvokeAsync(HttpContext context) {
        HttpRequest request = context.Request;
        string method = request.Method.ToUpperInvariant();
        string path = NormalizePath(request.Path);

        RateLimitPartition partition = _state.GetOrCreate(ResolveClientIp(context), method, path);

        // Orden de las reglas: primero la ventana corta (15s), después la de un minuto.
        using (RateLimitLease generalLease = await partition.General.AcquireAsync(1, context.RequestAborted)) {
            if (!generalLease.IsAcquired) {
                await RejectAsync(context, generalLease, RateLimitConfig.GeneralLimit, RateLimitConfig.GeneralWindow);
                return;
            }
        }

        if (partition.HasMinuteWindow) {
            using RateLimitLease minuteLease = await partition.Minute!.AcquireAsync(1, context.RequestAborted);
            if (!minuteLease.IsAcquired) {
                await RejectAsync(context, minuteLease, partition.MinuteLimit, RateLimitConfig.MinuteWindow);
                return;
            }
        }

        SetRateLimitHeaders(context, partition);
        await _next(context);
    }

    /// <summary>
    ///     Escribe las cabeceras estándar de rate limiting en la respuesta permitida,
    ///     usando la ventana de periodo más largo (la misma prioridad que las cabeceras
    ///     <c>X-Rate-Limit-*</c> clásicas).
    /// </summary>
    private static void SetRateLimitHeaders(HttpContext context, RateLimitPartition partition) {
        (FixedWindowRateLimiter limiter, int limit, TimeSpan window) = partition.HasMinuteWindow
            ? (partition.Minute!, partition.MinuteLimit, RateLimitConfig.MinuteWindow)
            : (partition.General, RateLimitConfig.GeneralLimit, RateLimitConfig.GeneralWindow);

        RateLimiterStatistics? statistics = limiter.GetStatistics();
        long remaining = statistics is null ? 0 : Math.Max(0, statistics.CurrentAvailablePermits);

        context.Response.Headers["RateLimit-Limit"] = limit.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["RateLimit-Remaining"] = remaining.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["RateLimit-Reset"] =
            ComputeResetSeconds(partition.CreatedUtc, window).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Calcula los segundos que faltan para el siguiente replenishment de la ventana
    ///     (el temporizador de la ventana fija se ancla en la creación del limitador).
    /// </summary>
    private static int ComputeResetSeconds(DateTime createdUtc, TimeSpan window) {
        TimeSpan elapsed = DateTime.UtcNow - createdUtc;
        if (elapsed < TimeSpan.Zero) {
            elapsed = TimeSpan.Zero;
        }

        long windowsElapsed = elapsed.Ticks / window.Ticks;
        DateTime nextReset = createdUtc.AddTicks((windowsElapsed + 1) * window.Ticks);
        int seconds = (int)Math.Ceiling((nextReset - DateTime.UtcNow).TotalSeconds);
        return Math.Max(0, seconds);
    }

    /// <summary>
    ///     Devuelve el 429 con cuerpo JSON y la cabecera <c>Retry-After</c>
    ///     (segundos hasta que se replenishen los permisos).
    /// </summary>
    private static async Task RejectAsync(HttpContext context, RateLimitLease lease, int limit, TimeSpan window) {
        int retryAfter = 1;
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan value) && value is TimeSpan remaining) {
            retryAfter = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
        }

        HttpResponse response = context.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
        response.ContentType = "application/json";

        var payload = new {
            message = "Demasiadas solicitudes. Por favor, intente más tarde.",
            errorType = "RateLimitError",
            timestamp = DateTime.UtcNow.ToString("o"),
            path = context.Request.Path.ToString(),
            method = context.Request.Method,
            limit,
            window = FormatWindow(window),
            retryAfter
        };

        await response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions), context.RequestAborted);
    }

    private static string FormatWindow(TimeSpan window) {
        return window.Minutes > 0 && window.Seconds == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{window.Minutes}m")
            : string.Create(CultureInfo.InvariantCulture, $"{window.Seconds}s");
    }

    /// <summary>
    ///     Resuelve la IP del cliente: cabeceras del proxy inverso
    ///     (<c>X-Forwarded-For</c> primer salto, luego <c>X-Real-IP</c>) con
    ///     fallback a la IP de conexión directa.
    /// </summary>
    private static string ResolveClientIp(HttpContext context) {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwarded) && forwarded.Count > 0) {
            string first = forwarded.ToString().Split(',')[0].Trim();
            if (first.Length > 0) {
                return first;
            }
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out StringValues realIp) && realIp.Count > 0) {
            string value = realIp.ToString().Trim();
            if (value.Length > 0) {
                return value;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>Normaliza la ruta igual que el rate limiting clásico: minúsculas y sin barra final.</summary>
    private static string NormalizePath(PathString path) {
        string value = path.Value?.ToLowerInvariant() ?? "/";
        return value.Length > 1 ? value.TrimEnd('/') : value;
    }
}
