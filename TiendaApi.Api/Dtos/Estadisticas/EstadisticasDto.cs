namespace TiendaApi.Api.Dtos.Estadisticas;

/// <summary>
/// Ventana temporal de una consulta estadística.
/// Los dos límites son opcionales: sin ellos se analiza todo el histórico.
/// </summary>
/// <example>{"desde": "2026-01-01T00:00:00Z", "hasta": null, "dias": 305}</example>
public record PeriodoDto(
    DateTime? Desde,
    DateTime? Hasta,
    int Dias
);

/// <summary>
/// Totales compartidos por las vistas de ranking y de serie.
/// </summary>
public record TotalesDto(
    int Pedidos,
    int Unidades,
    decimal Recaudacion,
    decimal TicketMedio
);

// ────────────────────────────────────────────────────────────────
// 1 · GET /api/estadisticas/resumen
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Resumen general: ventas, distribución de pedidos por estado, inventario y
/// comparativa con el periodo inmediatamente anterior de igual duración.
/// </summary>
public record ResumenEstadisticasDto(
    PeriodoDto Periodo,
    ResumenVentasDto Ventas,
    IReadOnlyDictionary<string, int> PedidosPorEstado,
    ResumenProductosDto Productos,
    ResumenCategoriasDto Categorias,
    ResumenUsuariosDto Usuarios,
    ComparativaDto? Comparativa
);

/// <summary>Cifras de venta del periodo.</summary>
public record ResumenVentasDto(
    int Pedidos,
    int Unidades,
    decimal Recaudacion,
    decimal TicketMedio,
    decimal UnidadesPorPedido,
    decimal RecaudacionPorDia,
    int ClientesDistintos
);

/// <summary>Cifras de inventario (productos activos del catálogo).</summary>
public record ResumenProductosDto(
    int Total,
    int Activos,
    int Borrados,
    int SinStock,
    int StockBajo,
    int StockTotal,
    decimal ValorInventario,
    decimal PrecioMedio,
    decimal PrecioMaximo
);

/// <summary>Cifras de catálogo de categorías.</summary>
public record ResumenCategoriasDto(
    int Total,
    int SinProductos
);

/// <summary>Cifras de usuarios: totales, compradores y altas del periodo.</summary>
public record ResumenUsuariosDto(
    int Total,
    int ConPedidos,
    int NuevosEnPeriodo
);

/// <summary>
/// Variación porcentual respecto del periodo anterior de la misma duración.
/// Null cuando no hay periodo anterior comparable.
/// </summary>
public record ComparativaDto(
    decimal Recaudacion,
    decimal Pedidos,
    decimal Unidades,
    decimal Clientes
);

// ────────────────────────────────────────────────────────────────
// 2 · GET /api/estadisticas/ventas
// ────────────────────────────────────────────────────────────────

/// <summary>Serie temporal de ventas agrupada por día, semana o mes.</summary>
public record VentasSerieDto(
    PeriodoDto Periodo,
    string Agrupar,
    IReadOnlyList<PuntoSerieDto> Series,
    TotalesDto Totales
);

/// <summary>Un punto de la serie: un periodo calendario con sus cifras.</summary>
public record PuntoSerieDto(
    string Periodo,
    int Pedidos,
    int Unidades,
    decimal Recaudacion,
    decimal TicketMedio
);

// ────────────────────────────────────────────────────────────────
// 3 · GET /api/estadisticas/productos-mas-vendidos
// ────────────────────────────────────────────────────────────────

/// <summary>Ranking de productos por unidades vendidas o por recaudación.</summary>
public record ProductosMasVendidosDto(
    PeriodoDto Periodo,
    string Orden,
    IReadOnlyList<FilaProductoVendidoDto> Productos,
    TotalesDto Totales
);

/// <summary>Una fila del ranking de productos.</summary>
public record FilaProductoVendidoDto(
    int Pos,
    long ProductoId,
    string Nombre,
    string Categoria,
    int Unidades,
    decimal Recaudacion,
    int Pedidos,
    decimal PorcentajeRecaudacion
);

// ────────────────────────────────────────────────────────────────
// 4 · GET /api/estadisticas/categorias
// ────────────────────────────────────────────────────────────────

/// <summary>Rendimiento de cada categoría en el periodo.</summary>
public record CategoriasEstadisticasDto(
    PeriodoDto Periodo,
    IReadOnlyList<FilaCategoriaDto> Categorias,
    TotalesDto Totales
);

/// <summary>Una fila del desglose por categoría. Las categorías sin ventas salen con ceros.</summary>
public record FilaCategoriaDto(
    long CategoriaId,
    string Nombre,
    int Productos,
    int ProductosVendidos,
    int Unidades,
    decimal Recaudacion,
    int Pedidos,
    decimal PorcentajeRecaudacion,
    decimal RecaudacionMediaPorProducto
);

// ────────────────────────────────────────────────────────────────
// 5 · GET /api/estadisticas/proveedores
// ────────────────────────────────────────────────────────────────

/// <summary>Rendimiento de cada proveedor en el periodo.</summary>
public record ProveedoresEstadisticasDto(
    PeriodoDto Periodo,
    IReadOnlyList<FilaProveedorDto> Proveedores,
    TotalesDto Totales
);

/// <summary>Una fila del desglose por proveedor.</summary>
public record FilaProveedorDto(
    string Codigo,
    string Nombre,
    int Productos,
    int Unidades,
    decimal Recaudacion,
    int Pedidos,
    decimal PorcentajeRecaudacion
);

// ────────────────────────────────────────────────────────────────
// 6 · GET /api/estadisticas/estados
// ────────────────────────────────────────────────────────────────

/// <summary>
/// Embudo de estados. Los cancelados se apartan: nunca suman en la recaudación
/// y se reportan con su propio importe no facturado.
/// </summary>
public record EstadosEstadisticasDto(
    PeriodoDto Periodo,
    IReadOnlyList<FilaEstadoDto> Estados,
    CanceladosDto Cancelados,
    TotalesDto Totales
);

/// <summary>Una fila del embudo de estados.</summary>
public record FilaEstadoDto(
    string Estado,
    int Pedidos,
    decimal Porcentaje,
    decimal Recaudacion
);

/// <summary>Pedidos cancelados y dinero que dejaron de facturar.</summary>
public record CanceladosDto(
    int Pedidos,
    decimal ImporteNoFacturado
);

// ────────────────────────────────────────────────────────────────
// 7 · GET /api/estadisticas/clientes
// ────────────────────────────────────────────────────────────────

/// <summary>Clientes con más gasto en el periodo.</summary>
public record ClientesEstadisticasDto(
    PeriodoDto Periodo,
    IReadOnlyList<FilaClienteDto> Clientes
);

/// <summary>Una fila del ranking de clientes.</summary>
public record FilaClienteDto(
    long UsuarioId,
    string Username,
    int Pedidos,
    int Unidades,
    decimal Gasto,
    decimal GastoMedio,
    DateTime? PrimeraCompra,
    DateTime? UltimaCompra,
    string ProductoFavorito
);

// ────────────────────────────────────────────────────────────────
// 8 · GET /api/estadisticas/inventario
// ────────────────────────────────────────────────────────────────

/// <summary>Estado del inventario: valor, agotados y productos con poco stock.</summary>
public record InventarioDto(
    int StockMax,
    InventarioResumenDto Productos,
    IReadOnlyList<FilaInventarioCategoriaDto> PorCategoria,
    IReadOnlyList<FilaAgotadoDto> Agotados,
    IReadOnlyList<FilaValorDto> MayorValor
);

/// <summary>Totales del inventario.</summary>
public record InventarioResumenDto(
    int Total,
    int Activos,
    int SinStock,
    int StockBajo,
    int StockTotal,
    decimal ValorInventario,
    decimal PrecioMedio
);

/// <summary>Inventario agrupado por categoría.</summary>
public record FilaInventarioCategoriaDto(
    long CategoriaId,
    string Nombre,
    int Productos,
    int Stock,
    decimal Valor
);

/// <summary>Un producto sin unidades disponibles.</summary>
public record FilaAgotadoDto(
    long ProductoId,
    string Nombre,
    int Stock,
    decimal Precio
);

/// <summary>Un producto ordenado por el valor monetario de su stock.</summary>
public record FilaValorDto(
    long ProductoId,
    string Nombre,
    int Stock,
    decimal Valor
);

// ────────────────────────────────────────────────────────────────
// 9 · GET /api/estadisticas/precios
// ────────────────────────────────────────────────────────────────

/// <summary>Evolución del precio de un producto leída del histórico de Cassandra.</summary>
public record PreciosEstadisticasDto(
    long ProductoId,
    int Puntos,
    DateTime? Desde,
    DateTime? Hasta,
    decimal Minimo,
    decimal Maximo,
    decimal Media,
    decimal Ultimo,
    decimal VariacionAbsoluta,
    decimal VariacionPorcentaje,
    IReadOnlyList<PuntoPrecioDto> Serie
);

/// <summary>Un punto del histórico de precios.</summary>
public record PuntoPrecioDto(
    DateTime Fecha,
    decimal Precio
);
