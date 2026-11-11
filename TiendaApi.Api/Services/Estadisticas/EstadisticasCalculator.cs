using TiendaApi.Api.Dtos.Estadisticas;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Services.Estadisticas;

/// <summary>
///     Cálculos estadísticos sobre pedidos, productos y precios.
///     🎓 Funciones puras: reciben datos ya cargados y devuelven DTOs, sin tocar ninguna
///     base de datos. Eso permite testear cada regla de negocio sin contenedores y
///     garantiza que todas las vistas (resumen, serie, rankings, embudo) compartan las
///     MISMAS reglas.
///     Reglas de negocio aplicadas:
///     <list type="bullet">
///         <item>
///             Recaudación: suma de <see cref="Pedido.Total" /> de los pedidos que no están
///             cancelados ni borrados lógicamente.
///         </item>
///         <item>Unidades: suma de <see cref="PedidoItem.Cantidad" /> en esos mismos pedidos.</item>
///         <item>
///             El embudo de estados sí muestra los cancelados, pero nunca suman en los totales:
///             se reportan aparte con su importe no facturado.
///         </item>
///         <item>Sin pedidos, el ticket medio es 0 (no NaN).</item>
///     </list>
/// </summary>
public static class EstadisticasCalculator {
    /// <summary>Agrupaciones admitidas en la serie temporal.</summary>
    public static readonly string[] Agrupaciones = ["dia", "semana", "mes"];

    /// <summary>
    ///     Crea la ventana de análisis y calcula su duración en días.
    /// </summary>
    public static PeriodoDto CrearPeriodo(DateTime desde, DateTime hasta) {
        return new PeriodoDto(desde, hasta, Math.Max(0, (hasta.Date - desde.Date).Days + 1));
    }

    /// <summary>
    ///     Filtra los pedidos de una ventana temporal. Los borrados lógicos quedan fuera.
    /// </summary>
    public static IEnumerable<Pedido> EnVentana(IEnumerable<Pedido> pedidos, DateTime desde, DateTime hasta) {
        return pedidos.Where(p => !p.IsDeleted && p.CreatedAt >= desde && p.CreatedAt < hasta);
    }

    /// <summary>Indica si un pedido cuenta para ingresos (todo salvo cancelado).</summary>
    public static bool CuentaParaIngresos(Pedido pedido) {
        return !pedido.IsDeleted && !string.Equals(pedido.Estado, PedidoEstado.CANCELADO, StringComparison.Ordinal);
    }

    /// <summary>Suma de unidades vendidas de una colección de pedidos.</summary>
    public static int SumarUnidades(IEnumerable<Pedido> pedidos) {
        return pedidos.Sum(p => p.Items?.Sum(i => i.Cantidad) ?? 0);
    }

    /// <summary>
    ///     Totales de venta de una colección de pedidos (sin cancelados).
    /// </summary>
    public static TotalesDto CalcularTotales(IEnumerable<Pedido> pedidos) {
        var validos = pedidos.Where(CuentaParaIngresos).ToList();
        decimal recaudacion = validos.Sum(p => p.Total);
        int unidades = SumarUnidades(validos);

        return new TotalesDto(
            validos.Count,
            unidades,
            recaudacion,
            validos.Count == 0 ? 0 : Math.Round(recaudacion / validos.Count, 2));
    }

    /// <summary>
    ///     Serie temporal por día, semana ISO o mes, con los periodos vacíos rellenos
    ///     a cero para que las gráficas no tengan huecos.
    /// </summary>
    public static IReadOnlyList<PuntoSerieDto> Serie(
        IEnumerable<Pedido> pedidos, string agrupar, DateTime desde, DateTime hasta) {
        var validos = pedidos.Where(CuentaParaIngresos).ToList();
        var puntos = new Dictionary<string, (int Pedidos, int Unidades, decimal Recaudacion)>();

        foreach (Pedido pedido in validos) {
            string clave = ClavePeriodo(pedido.CreatedAt, agrupar);
            int unidades = pedido.Items?.Sum(i => i.Cantidad) ?? 0;
            puntos[clave] = puntos.TryGetValue(clave, out (int Pedidos, int Unidades, decimal Recaudacion) acumulado)
                ? (acumulado.Pedidos + 1, acumulado.Unidades + unidades, acumulado.Recaudacion + pedido.Total)
                : (1, unidades, pedido.Total);
        }

        var salida = new List<PuntoSerieDto>();
        for (DateTime fecha = InicioPeriodo(desde, agrupar); fecha < hasta; fecha = SiguientePeriodo(fecha, agrupar)) {
            string clave = ClavePeriodo(fecha, agrupar);
            (int Pedidos, int Unidades, decimal Recaudacion) acumulado =
                puntos.TryGetValue(clave, out (int Pedidos, int Unidades, decimal Recaudacion) v)
                    ? v
                    : (Pedidos: 0, Unidades: 0, Recaudacion: 0m);
            salida.Add(new PuntoSerieDto(
                clave,
                acumulado.Pedidos,
                acumulado.Unidades,
                acumulado.Recaudacion,
                acumulado.Pedidos == 0 ? 0 : Math.Round(acumulado.Recaudacion / acumulado.Pedidos, 2)));
        }

        return salida;
    }

    /// <summary>
    ///     Clave de agrupación de una fecha: <c>2026-11-02</c>, <c>2026-W44</c> o <c>2026-11</c>.
    /// </summary>
    public static string ClavePeriodo(DateTime fecha, string agrupar) {
        return agrupar switch {
            "dia" => fecha.ToString("yyyy-MM-dd"),
            "semana" => $"{fecha:yyyy}-W{SemanaIso(fecha):00}",
            "mes" => fecha.ToString("yyyy-MM"),
            _ => fecha.ToString("yyyy-MM-dd")
        };
    }

    /// <summary>
    ///     Ranking de productos vendidos. El nombre sale del pedido (histórico): el producto
    ///     puede haberse borrado y la fila sigue siendo válida.
    /// </summary>
    public static IReadOnlyList<FilaProductoVendidoDto> RankingProductos(
        IEnumerable<Pedido> pedidos,
        IReadOnlyDictionary<long, Producto> productos,
        IReadOnlyDictionary<long, Categoria> categorias,
        string orden,
        int top) {
        // Cada línea se asocia a su pedido: así "Pedidos" cuenta pedidos distintos
        // y no líneas de pedido (un mismo producto puede aparecer varias veces).
        var lineas = pedidos
            .Where(CuentaParaIngresos)
            .SelectMany(p => (p.Items ?? []).Select(item => new { Item = item, Pedido = p }));

        var filas = lineas
            .GroupBy(x => x.Item.ProductoId)
            .Select(g => {
                int unidades = g.Sum(x => x.Item.Cantidad);
                decimal recaudacion = g.Sum(x => x.Item.Subtotal);
                long productoId = g.Key;
                productos.TryGetValue(productoId, out Producto? producto);
                string categoriaNombre =
                    producto is null || !categorias.TryGetValue(producto.CategoriaId, out Categoria? cat)
                        ? string.Empty
                        : cat.Nombre;
                return new {
                    ProductoId = productoId,
                    Nombre = producto?.Nombre ?? g.First().Item.NombreProducto,
                    Categoria = categoriaNombre,
                    Unidades = unidades,
                    Recaudacion = recaudacion,
                    Pedidos = g.Select(x => x.Pedido.Id).Distinct().Count()
                };
            })
            .ToList();

        decimal totalRecaudacion = filas.Sum(f => f.Recaudacion);
        var ordenados = orden == "recaudacion"
            ? filas.OrderByDescending(f => f.Recaudacion).ThenByDescending(f => f.Unidades)
            : filas.OrderByDescending(f => f.Unidades).ThenByDescending(f => f.Recaudacion);

        return ordenados
            .Take(Math.Clamp(top, 1, 50))
            .Select((f, i) => new FilaProductoVendidoDto(
                i + 1,
                f.ProductoId,
                f.Nombre,
                f.Categoria,
                f.Unidades,
                f.Recaudacion,
                f.Pedidos,
                totalRecaudacion == 0
                    ? 0
                    : Math.Round(f.Recaudacion * 100 / totalRecaudacion, 2)))
            .ToList();
    }

    /// <summary>
    ///     Desglose por categoría. Las categorías sin ventas se mantienen en la lista con
    ///     ceros, para que las gráficas muestren el catálogo completo.
    /// </summary>
    public static IReadOnlyList<FilaCategoriaDto> DesgloseCategorias(
        IEnumerable<Pedido> pedidos,
        IReadOnlyDictionary<long, Categoria> categorias,
        IReadOnlyDictionary<long, Producto> productos) {
        var porCategoria = pedidos
            .Where(CuentaParaIngresos)
            .SelectMany(p => (p.Items ?? []).Select(item => new { Item = item, Pedido = p }))
            .GroupBy(x => CategoriaDeProducto(x.Item.ProductoId, productos))
            .ToDictionary(g => g.Key, g => new {
                Unidades = g.Sum(x => x.Item.Cantidad),
                Recaudacion = g.Sum(x => x.Item.Subtotal),
                Pedidos = g.Select(x => x.Pedido.Id).Distinct().Count()
            });

        var productosPorCategoria = productos.Values
            .Where(p => !p.IsDeleted)
            .GroupBy(p => p.CategoriaId)
            .ToDictionary(g => g.Key, g => g.Count());

        var vendidosPorCategoria = pedidos
            .Where(CuentaParaIngresos)
            .SelectMany(p => p.Items ?? [])
            .Select(i => i.ProductoId)
            .Distinct()
            .GroupBy(id => CategoriaDeProducto(id, productos))
            .ToDictionary(g => g.Key, g => g.Count());

        decimal totalRecaudacion = porCategoria.Values.Sum(v => v.Recaudacion);

        return categorias.Values
            .Where(c => !c.IsDeleted)
            .Select(c => {
                porCategoria.TryGetValue(c.Id, out var venta);
                decimal recaudacion = venta?.Recaudacion ?? 0;
                int productosDeLaCategoria = productosPorCategoria.GetValueOrDefault(c.Id);
                return new FilaCategoriaDto(
                    c.Id,
                    c.Nombre,
                    productosDeLaCategoria,
                    vendidosPorCategoria.GetValueOrDefault(c.Id),
                    venta?.Unidades ?? 0,
                    recaudacion,
                    venta?.Pedidos ?? 0,
                    totalRecaudacion == 0 ? 0 : Math.Round(recaudacion * 100 / totalRecaudacion, 2),
                    productosDeLaCategoria == 0
                        ? 0
                        : Math.Round(recaudacion / productosDeLaCategoria, 2));
            })
            .OrderByDescending(c => c.Recaudacion)
            .ToList();
    }

    /// <summary>
    ///     Desglose por proveedor. Los productos sin proveedor se agrupan aparte con el
    ///     código vacío, para no perder ventas del recuento.
    /// </summary>
    public static IReadOnlyList<FilaProveedorDto> DesgloseProveedores(
        IEnumerable<Pedido> pedidos,
        IReadOnlyDictionary<string, Proveedor> proveedores,
        IReadOnlyDictionary<long, Producto> productos) {
        var filas = pedidos
            .Where(CuentaParaIngresos)
            .SelectMany(p => (p.Items ?? []).Select(item => new { Item = item, Pedido = p }))
            .GroupBy(x => ProveedorDeProducto(x.Item.ProductoId, productos))
            .Select(g => {
                string codigo = g.Key;
                proveedores.TryGetValue(codigo, out Proveedor? proveedor);
                return new {
                    Codigo = codigo,
                    Nombre = proveedor?.Nombre ?? string.Empty,
                    Unidades = g.Sum(x => x.Item.Cantidad),
                    Recaudacion = g.Sum(x => x.Item.Subtotal),
                    Pedidos = g.Select(x => x.Pedido.Id).Distinct().Count(),
                    Productos = g.Select(x => x.Item.ProductoId).Distinct().Count()
                };
            })
            .ToList();

        decimal totalRecaudacion = filas.Sum(f => f.Recaudacion);

        return filas
            .OrderByDescending(f => f.Recaudacion)
            .Select(f => new FilaProveedorDto(
                f.Codigo,
                f.Nombre,
                f.Productos,
                f.Unidades,
                f.Recaudacion,
                f.Pedidos,
                totalRecaudacion == 0
                    ? 0
                    : Math.Round(f.Recaudacion * 100 / totalRecaudacion, 2)))
            .ToList();
    }

    /// <summary>
    ///     Ranking de clientes por gasto. El producto favorito es el que más unidades
    ///     compró en la ventana.
    /// </summary>
    public static IReadOnlyList<FilaClienteDto> RankingClientes(
        IEnumerable<Pedido> pedidos,
        IReadOnlyDictionary<long, string> usuarios,
        int top) {
        var filas = pedidos
            .Where(CuentaParaIngresos)
            .GroupBy(p => p.UserId)
            .Select(g => {
                var items = g.SelectMany(p => p.Items ?? []).ToList();
                var favorito = items
                    .GroupBy(i => i.ProductoId)
                    .Select(x => new
                        { ProductoId = x.Key, Nombre = x.First().NombreProducto, Unidades = x.Sum(i => i.Cantidad) })
                    .OrderByDescending(x => x.Unidades)
                    .FirstOrDefault();
                decimal gasto = g.Sum(p => p.Total);

                return new FilaClienteDto(
                    g.Key,
                    usuarios.GetValueOrDefault(g.Key, string.Empty),
                    g.Count(),
                    items.Sum(i => i.Cantidad),
                    gasto,
                    Math.Round(gasto / g.Count(), 2),
                    g.Min(p => p.CreatedAt),
                    g.Max(p => p.CreatedAt),
                    favorito?.Nombre ?? string.Empty);
            })
            .OrderByDescending(c => c.Gasto)
            .Take(Math.Clamp(top, 1, 50))
            .ToList();

        return filas;
    }

    /// <summary>
    ///     Recuento de pedidos por estado (incluye los cancelados: es un embudo).
    /// </summary>
    public static IReadOnlyDictionary<string, int> PedidosPorEstado(IEnumerable<Pedido> pedidos) {
        return pedidos
            .Where(p => !p.IsDeleted)
            .GroupBy(p => p.Estado)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    ///     Embudo de estados con porcentajes sobre el total y cancelados aparte.
    /// </summary>
    public static EstadosEstadisticasDto EmbudoEstados(IEnumerable<Pedido> pedidos, PeriodoDto periodo) {
        var todos = pedidos.Where(p => !p.IsDeleted).ToList();
        int total = todos.Count;
        var cancelados = todos.Where(p => string.Equals(p.Estado, PedidoEstado.CANCELADO, StringComparison.Ordinal))
            .ToList();
        var validos = todos.Where(CuentaParaIngresos).ToList();

        var estados = todos
            .GroupBy(p => p.Estado)
            .Select(g => new FilaEstadoDto(
                g.Key,
                g.Count(),
                total == 0 ? 0 : Math.Round(g.Count() * 100m / total, 2),
                g.Where(CuentaParaIngresos).Sum(p => p.Total)))
            .OrderByDescending(e => e.Pedidos)
            .ToList();

        return new EstadosEstadisticasDto(
            periodo,
            estados,
            new CanceladosDto(cancelados.Count, cancelados.Sum(p => p.Total)),
            CalcularTotales(validos));
    }

    /// <summary>
    ///     Variación porcentual de cada métrica respecto del periodo anterior.
    ///     Devuelve null cuando el periodo anterior no tiene datos que comparar.
    /// </summary>
    public static ComparativaDto? CalcularComparativa(TotalesDto actuales, TotalesDto anteriores, int clientesActuales,
        int clientesAnteriores) {
        if (anteriores.Pedidos == 0 && anteriores.Unidades == 0 && anteriores.Recaudacion == 0) {
            return null;
        }

        return new ComparativaDto(
            Variacion(actuales.Recaudacion, anteriores.Recaudacion),
            Variacion(actuales.Pedidos, anteriores.Pedidos),
            Variacion(actuales.Unidades, anteriores.Unidades),
            Variacion(clientesActuales, clientesAnteriores));
    }

    /// <summary>Estado del inventario a partir del catálogo de productos activos.</summary>
    public static InventarioDto Inventario(
        IReadOnlyDictionary<long, Producto> productos,
        IReadOnlyDictionary<long, Categoria> categorias,
        int stockMax) {
        var activos = productos.Values.Where(p => !p.IsDeleted).ToList();
        int umbral = Math.Max(0, stockMax);

        var porCategoria = activos
            .GroupBy(p => p.CategoriaId)
            .Select(g => {
                categorias.TryGetValue(g.Key, out Categoria? categoria);
                return new FilaInventarioCategoriaDto(
                    g.Key,
                    categoria?.Nombre ?? string.Empty,
                    g.Count(),
                    g.Sum(p => p.Stock),
                    Math.Round(g.Sum(p => p.Precio * p.Stock), 2));
            })
            .OrderByDescending(c => c.Valor)
            .ToList();

        return new InventarioDto(
            umbral,
            new InventarioResumenDto(
                activos.Count,
                activos.Count,
                activos.Count(p => p.Stock == 0),
                activos.Count(p => p.Stock > 0 && p.Stock <= umbral),
                activos.Sum(p => p.Stock),
                Math.Round(activos.Sum(p => p.Precio * p.Stock), 2),
                activos.Count == 0 ? 0 : Math.Round(activos.Average(p => p.Precio), 2)),
            porCategoria,
            activos
                .Where(p => p.Stock == 0)
                .OrderBy(p => p.Nombre)
                .Select(p => new FilaAgotadoDto(p.Id, p.Nombre, p.Stock, p.Precio))
                .ToList(),
            activos
                .OrderByDescending(p => p.Precio * p.Stock)
                .Take(10)
                .Select(p => new FilaValorDto(p.Id, p.Nombre, p.Stock, Math.Round(p.Precio * p.Stock, 2)))
                .ToList());
    }

    /// <summary>
    ///     Estadísticas de una serie de precios ya cargada del histórico.
    /// </summary>
    public static PreciosEstadisticasDto Precios(
        long productoId, IReadOnlyList<PuntoPrecioDto> serie, DateTime? desde, DateTime? hasta) {
        var puntos = serie.OrderBy(p => p.Fecha).ToList();
        if (puntos.Count == 0) {
            return new PreciosEstadisticasDto(productoId, 0, desde, hasta, 0, 0, 0, 0, 0, 0, []);
        }

        decimal minimo = puntos.Min(p => p.Precio);
        decimal maximo = puntos.Max(p => p.Precio);
        decimal media = Math.Round(puntos.Average(p => p.Precio), 2);
        decimal ultimo = puntos[^1].Precio;
        decimal primero = puntos[0].Precio;

        return new PreciosEstadisticasDto(
            productoId,
            puntos.Count,
            desde,
            hasta,
            minimo,
            maximo,
            media,
            ultimo,
            Math.Round(ultimo - primero, 2),
            primero == 0 ? 0 : Math.Round((ultimo - primero) * 100 / primero, 2),
            puntos);
    }

    /// <summary>
    ///     Semana ISO 8601 de una fecha: la semana que contiene el jueves, con el
    ///     lunes como primer día (semana 1 = la que contiene el 4 de enero).
    /// </summary>
    public static int SemanaIso(DateTime fecha) {
        int dia = (int)fecha.DayOfWeek;
        if (dia == 0) {
            dia = 7; // lunes = 1 … domingo = 7
        }

        DateTime jueves = fecha.AddDays(4 - dia); // jueves de la semana de la fecha
        var inicioAno = new DateTime(jueves.Year, 1, 1);
        return (int)Math.Ceiling(((jueves - inicioAno).Days + 1) / 7.0);
    }

    private static DateTime InicioPeriodo(DateTime fecha, string agrupar) {
        return agrupar switch {
            "semana" => InicioSemana(fecha),
            "mes" => new DateTime(fecha.Year, fecha.Month, 1),
            _ => fecha.Date
        };
    }

    private static DateTime SiguientePeriodo(DateTime fecha, string agrupar) {
        return agrupar switch {
            "semana" => InicioSemana(fecha).AddDays(7),
            "mes" => fecha.AddMonths(1),
            _ => fecha.AddDays(1)
        };
    }

    private static DateTime InicioSemana(DateTime fecha) {
        int diferencia = ((int)fecha.DayOfWeek + 6) % 7; // lunes = 0
        return fecha.Date.AddDays(-diferencia);
    }

    private static long CategoriaDeProducto(long productoId, IReadOnlyDictionary<long, Producto> productos) {
        return productos.TryGetValue(productoId, out Producto? producto) ? producto.CategoriaId : 0;
    }

    private static string ProveedorDeProducto(long productoId, IReadOnlyDictionary<long, Producto> productos) {
        return productos.TryGetValue(productoId, out Producto? producto) && producto.ProveedorId is not null
            ? producto.ProveedorId
            : string.Empty;
    }

    private static decimal Variacion(decimal actual, decimal anterior) {
        return anterior == 0 ? 0 : Math.Round((actual - anterior) * 100 / anterior, 2);
    }
}
