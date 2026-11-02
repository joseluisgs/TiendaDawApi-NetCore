using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Estadisticas;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Estadisticas;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Proveedores;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Api.Services.Estadisticas;

/// <summary>
/// Servicio de estadísticas.
///
/// 🎓 Flujo de todas las vistas: resolver la ventana → cargar los pedidos de esa
/// ventana (consulta acotada cuando se pide rango) → pasarlos por
/// <see cref="EstadisticasCalculator"/> → devolver el DTO. Las catálogos
/// (productos, categorías, proveedores y usuarios) se cargan solo cuando la vista
/// necesita cruzarlos.
/// </summary>
public class EstadisticasService(
    IPedidosRepository pedidosRepository,
    IProductoRepository productoRepository,
    ICategoriaRepository categoriaRepository,
    IProveedorRepository proveedorRepository,
    IUserRepository userRepository,
    IHistorialService historialService,
    ILogger<EstadisticasService> logger
) : IEstadisticasService
{
    /// <summary>Puntos máximos del histórico de precios (límite del repositorio de Cassandra).</summary>
    private const int LimiteHistorialPrecios = 200;

    /// <inheritdoc/>
    public async Task<Result<ResumenEstadisticasDto, DomainError>> ResumenAsync(DateTime? desde, DateTime? hasta)
    {
        var (pedidos, periodo) = await CargarAsync(desde, hasta);
        var lista = pedidos.ToList();

        var totales = EstadisticasCalculator.CalcularTotales(lista);
        var clientesActuales = ClientesDistintos(lista);

        // Comparativa: la ventana inmediatamente anterior de la misma duración.
        ComparativaDto? comparativa = null;
        if (periodo.Dias > 0)
        {
            var anteriorDesde = periodo.Desde!.Value.AddDays(-periodo.Dias);
            var pedidosAnteriores = await CargarSoloRangoAsync(anteriorDesde, periodo.Desde.Value);
            var totalesAnteriores = EstadisticasCalculator.CalcularTotales(pedidosAnteriores);
            comparativa = EstadisticasCalculator.CalcularComparativa(
                totales, totalesAnteriores, clientesActuales, ClientesDistintos(pedidosAnteriores));
        }

        var (productos, categorias, _, _) = await CargarCatalogosAsync();

        var recaudacionPorDia = periodo.Dias == 0 ? 0 : Math.Round(totales.Recaudacion / periodo.Dias, 2);

        var resumen = new ResumenEstadisticasDto(
            Periodo: periodo,
            Ventas: new ResumenVentasDto(
                Pedidos: totales.Pedidos,
                Unidades: totales.Unidades,
                Recaudacion: totales.Recaudacion,
                TicketMedio: totales.TicketMedio,
                UnidadesPorPedido: totales.Pedidos == 0 ? 0 : Math.Round((decimal)totales.Unidades / totales.Pedidos, 2),
                RecaudacionPorDia: recaudacionPorDia,
                ClientesDistintos: clientesActuales),
            PedidosPorEstado: EstadisticasCalculator.PedidosPorEstado(lista),
            Productos: await ResumenProductosAsync(),
            Categorias: new ResumenCategoriasDto(
                Total: categorias.Count,
                SinProductos: categorias.Values
                    .Count(c => !productos.Values.Any(p => p.CategoriaId == c.Id))),
            Usuarios: await ResumenUsuariosAsync(periodo, lista),
            Comparativa: comparativa);

        logger.LogInformation(
            "Estadísticas de resumen: {Pedidos} pedidos y {Recaudacion} de recaudación en {Dias} días",
            resumen.Ventas.Pedidos, resumen.Ventas.Recaudacion, periodo.Dias);

        return Result.Success<ResumenEstadisticasDto, DomainError>(resumen);
    }

    /// <inheritdoc/>
    public async Task<Result<VentasSerieDto, DomainError>> VentasAsync(
        DateTime? desde, DateTime? hasta, string agrupar)
    {
        if (!EstadisticasCalculator.Agrupaciones.Contains(agruparCorrecto(agrupar)))
        {
            return Result.Failure<VentasSerieDto, DomainError>(
                ErrorEstadisticas.AgrupacionInvalida(agrupar));
        }

        var (pedidos, periodo) = await CargarAsync(desde, hasta);
        var serie = EstadisticasCalculator.Serie(pedidos, agrupar, periodo.Desde!.Value, periodo.Hasta!.Value);

        return Result.Success<VentasSerieDto, DomainError>(
            new VentasSerieDto(periodo, agrupar, serie, EstadisticasCalculator.CalcularTotales(pedidos)));
    }

    /// <inheritdoc/>
    public async Task<Result<ProductosMasVendidosDto, DomainError>> ProductosMasVendidosAsync(
        DateTime? desde, DateTime? hasta, string orden, int top)
    {
        var ordenLimpio = orden == "recaudacion" ? "recaudacion" : "unidades";
        var (pedidos, periodo) = await CargarAsync(desde, hasta);
        var (productos, categorias, _, _) = await CargarCatalogosAsync();

        var filas = EstadisticasCalculator.RankingProductos(
            pedidos, productos, categorias, ordenLimpio, top);

        return Result.Success<ProductosMasVendidosDto, DomainError>(
            new ProductosMasVendidosDto(periodo, ordenLimpio, filas, EstadisticasCalculator.CalcularTotales(pedidos)));
    }

    /// <inheritdoc/>
    public async Task<Result<CategoriasEstadisticasDto, DomainError>> CategoriasAsync(
        DateTime? desde, DateTime? hasta)
    {
        var (pedidos, periodo) = await CargarAsync(desde, hasta);
        var (productos, categorias, _, _) = await CargarCatalogosAsync();

        var filas = EstadisticasCalculator.DesgloseCategorias(pedidos, categorias, productos);

        return Result.Success<CategoriasEstadisticasDto, DomainError>(
            new CategoriasEstadisticasDto(periodo, filas, EstadisticasCalculator.CalcularTotales(pedidos)));
    }

    /// <inheritdoc/>
    public async Task<Result<ProveedoresEstadisticasDto, DomainError>> ProveedoresAsync(
        DateTime? desde, DateTime? hasta)
    {
        var (pedidos, periodo) = await CargarAsync(desde, hasta);
        var (productos, _, proveedores, _) = await CargarCatalogosAsync();

        var filas = EstadisticasCalculator.DesgloseProveedores(pedidos, proveedores, productos);

        return Result.Success<ProveedoresEstadisticasDto, DomainError>(
            new ProveedoresEstadisticasDto(periodo, filas, EstadisticasCalculator.CalcularTotales(pedidos)));
    }

    /// <inheritdoc/>
    public async Task<Result<EstadosEstadisticasDto, DomainError>> EstadosAsync(
        DateTime? desde, DateTime? hasta)
    {
        var (pedidos, periodo) = await CargarAsync(desde, hasta);
        return Result.Success<EstadosEstadisticasDto, DomainError>(
            EstadisticasCalculator.EmbudoEstados(pedidos, periodo));
    }

    /// <inheritdoc/>
    public async Task<Result<ClientesEstadisticasDto, DomainError>> ClientesAsync(
        DateTime? desde, DateTime? hasta, int top)
    {
        var (pedidos, periodo) = await CargarAsync(desde, hasta);
        var (_, _, _, usuarios) = await CargarCatalogosAsync();

        var filas = EstadisticasCalculator.RankingClientes(pedidos, usuarios, top);

        return Result.Success<ClientesEstadisticasDto, DomainError>(
            new ClientesEstadisticasDto(periodo, filas));
    }

    /// <inheritdoc/>
    public async Task<Result<InventarioDto, DomainError>> InventarioAsync(int stockMax)
    {
        var (productos, categorias, _, _) = await CargarCatalogosAsync();

        return Result.Success<InventarioDto, DomainError>(
            EstadisticasCalculator.Inventario(productos, categorias, stockMax));
    }

    /// <inheritdoc/>
    public async Task<Result<PreciosEstadisticasDto, DomainError>> PreciosAsync(
        long productoId, DateTime? desde, DateTime? hasta)
    {
        var historial = await historialService.GetHistorialPreciosAsync(
            productoId, desde, hasta, LimiteHistorialPrecios);

        if (historial.IsFailure)
            return Result.Failure<PreciosEstadisticasDto, DomainError>(historial.Error);

        // El repositorio devuelve la serie de más reciente a más antigua; la
        // calculadora la ordena ascendente para calcular la variación.
        var puntos = historial.Value
            .Select(h => new PuntoPrecioDto(h.Fecha, (decimal)Math.Round(h.Precio, 2)))
            .ToList();

        return Result.Success<PreciosEstadisticasDto, DomainError>(
            EstadisticasCalculator.Precios(productoId, puntos, desde, hasta));
    }

    // ────────────────────────────────────────────────────────────
    // Carga de datos
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Carga los pedidos de la ventana pedida. Si falta algún límite, se usan los
    /// extremos de los propios datos (histórico completo) y se carga todo.
    /// </summary>
    private async Task<(IEnumerable<Pedido> Pedidos, PeriodoDto Periodo)> CargarAsync(
        DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue && hasta.HasValue)
        {
            var pedidos = await pedidosRepository.FindByRangoFechasAsync(desde.Value, hasta.Value);
            return (pedidos, EstadisticasCalculator.CrearPeriodo(desde.Value, hasta.Value));
        }

        var todos = (await pedidosRepository.FindAllAsync()).ToList();
        var inicio = desde ?? (todos.Count > 0 ? todos.Min(p => p.CreatedAt).Date : DateTime.UtcNow.Date);
        var fin = hasta ?? DateTime.UtcNow.Date.AddDays(1);

        return (
            todos.Where(p => p.CreatedAt >= inicio && p.CreatedAt < fin),
            EstadisticasCalculator.CrearPeriodo(inicio, fin));
    }

    /// <summary>Carga solo los pedidos de un rango concreto (para la comparativa).</summary>
    private async Task<IEnumerable<Pedido>> CargarSoloRangoAsync(DateTime desde, DateTime hasta)
    {
        if (desde >= hasta) return [];
        return await pedidosRepository.FindByRangoFechasAsync(desde, hasta);
    }

    /// <summary>
    /// Catálogos que cruzan las vistas (productos, categorías, proveedores y usuarios),
    /// indexados por su clave.
    /// </summary>
    private async Task<(
        IReadOnlyDictionary<long, Producto> Productos,
        IReadOnlyDictionary<long, Categoria> Categorias,
        IReadOnlyDictionary<string, Proveedor> Proveedores,
        IReadOnlyDictionary<long, string> Usuarios)> CargarCatalogosAsync()
    {
        var productos = (await productoRepository.FindAllAsync()).ToDictionary(p => p.Id);
        var categorias = (await categoriaRepository.FindAllAsync()).ToDictionary(c => c.Id);
        var proveedores = (await proveedorRepository.FindAllAsync()).ToDictionary(p => p.Codigo);
        var usuarios = (await userRepository.FindAllAsync())
            .ToDictionary(u => u.Id, u => u.UserName ?? string.Empty);

        return (productos, categorias, proveedores, usuarios);
    }

    /// <summary>Cifras de inventario del catálogo actual.</summary>
    private async Task<ResumenProductosDto> ResumenProductosAsync()
    {
        var (productos, _, _, _) = await CargarCatalogosAsync();
        var activos = productos.Values.Where(p => !p.IsDeleted).ToList();

        // Los borrados no salen del repositorio (filtro global): se piden aparte
        // por su contador, que sí los incluye.
        var borrados = (await productoRepository.FindAllPagedAsync(new ProductoFilterDto(
            Nombre: null,
            Categoria: null,
            IsDeleted: true,
            PrecioMax: null,
            StockMin: null,
            Page: 0,
            Size: 1,
            SortBy: "id",
            Direction: "asc"))).TotalCount;

        return new ResumenProductosDto(
            Total: activos.Count + borrados,
            Activos: activos.Count,
            Borrados: borrados,
            SinStock: activos.Count(p => p.Stock == 0),
            StockBajo: activos.Count(p => p.Stock > 0 && p.Stock <= 5),
            StockTotal: activos.Sum(p => p.Stock),
            ValorInventario: Math.Round(activos.Sum(p => p.Precio * p.Stock), 2),
            PrecioMedio: activos.Count == 0 ? 0 : Math.Round(activos.Average(p => p.Precio), 2),
            PrecioMaximo: activos.Count == 0 ? 0 : activos.Max(p => p.Precio));
    }

    /// <summary>Cifras de usuarios, con los que compraron en la ventana.</summary>
    private async Task<ResumenUsuariosDto> ResumenUsuariosAsync(PeriodoDto periodo, IEnumerable<Pedido> pedidos)
    {
        var usuarios = (await userRepository.FindAllAsync()).ToList();

        var conPedidos = pedidos.Where(EstadisticasCalculator.CuentaParaIngresos)
            .Select(p => p.UserId)
            .Distinct()
            .Count();

        return new ResumenUsuariosDto(
            Total: usuarios.Count,
            ConPedidos: conPedidos,
            NuevosEnPeriodo: periodo.Desde is null
                ? 0
                : usuarios.Count(u => u.CreatedAt >= periodo.Desde && u.CreatedAt < periodo.Hasta));
    }

    /// <summary>Clientes distintos con compras en la ventana.</summary>
    private static int ClientesDistintos(IEnumerable<Pedido> pedidos) =>
        pedidos.Where(EstadisticasCalculator.CuentaParaIngresos)
            .Select(p => p.UserId)
            .Distinct()
            .Count();

    /// <summary>Normaliza la agrupación: se acepta en cualquier mayúscula/minúscula.</summary>
    private static string agruparCorrecto(string agrupar) => (agrupar ?? "mes").Trim().ToLowerInvariant();
}
