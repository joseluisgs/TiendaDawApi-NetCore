using FluentAssertions;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Estadisticas;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Estadisticas;

namespace TiendaApi.Tests.Unit.Services.Estadisticas;

/// <summary>
/// Tests unitarios de EstadisticasCalculator: las reglas de negocio de todas las
/// vistas estadísticas, sin tocar ninguna base de datos.
/// </summary>
[TestFixture]
public class EstadisticasCalculatorTests
{
    private static readonly DateTime Referencia = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static PedidoItem Item(long productoId, int cantidad, decimal precio, string nombre = "Producto") =>
        new()
        {
            ProductoId = productoId,
            NombreProducto = nombre,
            Cantidad = cantidad,
            Precio = precio,
            Subtotal = cantidad * precio,
        };

    private static Pedido Pedido(
        long id,
        decimal total,
        string estado = PedidoEstado.ENTREGADO,
        DateTime? creado = null,
        params PedidoItem[] items) =>
        new()
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId(),
            UserId = id,
            Total = total,
            Estado = estado,
            CreatedAt = creado ?? Referencia,
            IsDeleted = false,
            Items = [.. items],
        };

    private static Producto Producto(long id, long categoriaId, decimal precio = 10m, int stock = 5, string? proveedorId = null) =>
        new()
        {
            Id = id,
            Nombre = $"Producto {id}",
            Precio = precio,
            Stock = stock,
            CategoriaId = categoriaId,
            ProveedorId = proveedorId,
            IsDeleted = false,
        };

    // ────────────────────────────────────────────────────────────
    // Totales y reglas de ingresos
    // ────────────────────────────────────────────────────────────

    [Test]
    public void CalcularTotales_ExcluyeCanceladosYSumaUnidades()
    {
        var pedidos = new[]
        {
            Pedido(1, 100m, PedidoEstado.ENTREGADO, items: [Item(1, 2, 50m)]),
            Pedido(2, 200m, PedidoEstado.PENDIENTE, items: [Item(1, 4, 50m)]),
            Pedido(3, 999m, PedidoEstado.CANCELADO, items: [Item(1, 99, 10m)]),
        };

        var totales = EstadisticasCalculator.CalcularTotales(pedidos);

        totales.Pedidos.Should().Be(2, "el cancelado no cuenta");
        totales.Unidades.Should().Be(6);
        totales.Recaudacion.Should().Be(300m);
        totales.TicketMedio.Should().Be(150m);
    }

    [Test]
    public void CalcularTotales_SinPedidos_DevuelveCerosYNuncaNaN()
    {
        var totales = EstadisticasCalculator.CalcularTotales([]);

        totales.Pedidos.Should().Be(0);
        totales.Recaudacion.Should().Be(0m);
        totales.TicketMedio.Should().Be(0m, "sin pedidos el ticket medio es 0, no NaN");
    }

    [Test]
    public void EnVentana_FiltraPorFechasYExcluyeBorrados()
    {
        var dentro = Pedido(1, 10m, creado: new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));
        var fuera = Pedido(2, 10m, creado: new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc));
        var borrado = Pedido(3, 10m, creado: new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc));
        borrado.IsDeleted = true;

        var resultado = EstadisticasCalculator.EnVentana(
            new[] { dentro, fuera, borrado },
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));

        resultado.Should().ContainSingle().Which.Should().Be(dentro);
    }

    // ────────────────────────────────────────────────────────────
    // Serie temporal
    // ────────────────────────────────────────────────────────────

    [Test]
    public void Serie_RellenaLosPeriodosVaciosACero()
    {
        var desde = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc);
        var pedidos = new[] { Pedido(1, 50m, creado: new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc), items: [Item(1, 1, 50m)]) };

        var serie = EstadisticasCalculator.Serie(pedidos, "dia", desde, hasta);

        serie.Should().HaveCount(3, "del 1 al 3 de octubre (la ventana es exclusiva al final)");
        serie[0].Periodo.Should().Be("2026-10-01");
        serie[0].Pedidos.Should().Be(1);
        serie[0].Recaudacion.Should().Be(50m);
        serie[1].Pedidos.Should().Be(0, "los días sin ventas salen a cero");
        serie[1].Recaudacion.Should().Be(0m);
        serie[2].Pedidos.Should().Be(0);
    }

    [Test]
    public void Serie_AgrupadaPorMes_AcumulaLosDiasDelMes()
    {
        var desde = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var pedidos = new[]
        {
            Pedido(1, 10m, creado: new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), items: [Item(1, 1, 10m)]),
            Pedido(2, 20m, creado: new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), items: [Item(1, 2, 10m)]),
            Pedido(3, 30m, creado: new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc), items: [Item(1, 3, 10m)]),
        };

        var serie = EstadisticasCalculator.Serie(pedidos, "mes", desde, hasta);

        serie.Should().HaveCount(2);
        serie[0].Periodo.Should().Be("2026-09");
        serie[0].Pedidos.Should().Be(2);
        serie[0].Unidades.Should().Be(3);
        serie[0].Recaudacion.Should().Be(30m);
        serie[1].Periodo.Should().Be("2026-10");
        serie[1].Pedidos.Should().Be(1);
    }

    [Test]
    public void ClavePeriodo_SemanaUsaLaSemanaIso()
    {
        // 1 de enero de 2026 cae en jueves: pertenece a la semana 1.
        EstadisticasCalculator.ClavePeriodo(new DateTime(2026, 1, 1), "semana").Should().Be("2026-W01");
        // 4 de noviembre de 2026 es miércoles de la semana 45.
        EstadisticasCalculator.ClavePeriodo(new DateTime(2026, 11, 4), "semana").Should().Be("2026-W45");
    }

    // ────────────────────────────────────────────────────────────
    // Rankings y desgloses
    // ────────────────────────────────────────────────────────────

    [Test]
    public void RankingProductos_OrdenaPorUnidadesYPorcentuaSobreElTotal()
    {
        var pedidos = new[]
        {
            Pedido(1, 100m, items: [Item(1, 10, 10m, "Caro")]),
            Pedido(2, 30m, items: [Item(2, 30, 1m, "Barato")]),
        };
        var productos = new Dictionary<long, Producto>
        {
            [1] = Producto(1, categoriaId: 1, precio: 10m),
            [2] = Producto(2, categoriaId: 1, precio: 1m),
        };
        var categorias = new Dictionary<long, Categoria>
        {
            [1] = new() { Id = 1, Nombre = "General" },
        };

        var ranking = EstadisticasCalculator.RankingProductos(pedidos, productos, categorias, "unidades", 10);

        ranking.Should().HaveCount(2);
        ranking[0].ProductoId.Should().Be(2, "30 unidades vencen a 10");
        ranking[0].Unidades.Should().Be(30);
        ranking[0].Pedidos.Should().Be(1, "cuenta pedidos distintos, no líneas");
        ranking[0].PorcentajeRecaudacion.Should().Be(23.08m, "30 de 130");

        var porRecaudacion = EstadisticasCalculator.RankingProductos(pedidos, productos, categorias, "recaudacion", 10);
        porRecaudacion[0].ProductoId.Should().Be(1, "100 de recaudación vencen a 30");
    }

    [Test]
    public void DesgloseCategorias_LasCategoriasSinVentasSalenConCeros()
    {
        var pedidos = new[] { Pedido(1, 50m, items: [Item(1, 5, 10m)]) };
        var productos = new Dictionary<long, Producto> { [1] = Producto(1, categoriaId: 1) };
        var categorias = new Dictionary<long, Categoria>
        {
            [1] = new() { Id = 1, Nombre = "Con ventas" },
            [2] = new() { Id = 2, Nombre = "Sin ventas" },
        };

        var filas = EstadisticasCalculator.DesgloseCategorias(pedidos, categorias, productos);

        filas.Should().HaveCount(2, "el catálogo completo, no solo lo vendido");
        filas[0].Nombre.Should().Be("Con ventas", "las que venden van primero");
        filas[1].Nombre.Should().Be("Sin ventas");
        filas[1].Recaudacion.Should().Be(0m);
        filas[1].PorcentajeRecaudacion.Should().Be(0m);
    }

    [Test]
    public void DesgloseProveedores_AgrupaLoSinProveedorConCodigoVacio()
    {
        var pedidos = new[]
        {
            Pedido(1, 40m, items: [Item(1, 4, 10m), Item(2, 2, 5m)]),
        };
        var productos = new Dictionary<long, Producto>
        {
            [1] = Producto(1, categoriaId: 1, proveedorId: "Dist0000001"),
            [2] = Producto(2, categoriaId: 1, proveedorId: null),
        };
        var proveedores = new Dictionary<string, Proveedor>
        {
            ["Dist0000001"] = new() { Codigo = "Dist0000001", Nombre = "Distribuidor" },
        };

        var filas = EstadisticasCalculator.DesgloseProveedores(pedidos, proveedores, productos);

        filas.Should().HaveCount(2);
        filas[0].Codigo.Should().Be("Dist0000001");
        filas[0].Recaudacion.Should().Be(40m);
        filas[1].Codigo.Should().BeEmpty("lo sin proveedor queda aparte, sin perder la venta");
        filas[1].Recaudacion.Should().Be(10m);
    }

    [Test]
    public void RankingClientes_OrdenaPorGastoYCalculaElProductoFavorito()
    {
        var pedidos = new[]
        {
            Pedido(7, 10m, creado: Referencia, items: [Item(1, 1, 10m, "Funko")]),
            Pedido(7, 20m, creado: Referencia.AddDays(1), items: [Item(2, 2, 10m, "Funko"), Item(3, 1, 8m, "Camiseta")]),
            Pedido(9, 5m, creado: Referencia, items: [Item(3, 1, 5m, "Camiseta")]),
        };
        var usuarios = new Dictionary<long, string> { [7] = "userdaw", [9] = "otrousuario" };

        var filas = EstadisticasCalculator.RankingClientes(pedidos, usuarios, 10);

        filas.Should().HaveCount(2);
        filas[0].Username.Should().Be("userdaw");
        filas[0].Gasto.Should().Be(30m);
        filas[0].Pedidos.Should().Be(2);
        filas[0].ProductoFavorito.Should().Be("Funko", "2 unidades frente a 1");
    }

    // ────────────────────────────────────────────────────────────
    // Embudo, comparativa, inventario y precios
    // ────────────────────────────────────────────────────────────

    [Test]
    public void EmbudoEstados_MuestraLosCanceladosPeroNoLosSuma()
    {
        var periodo = EstadisticasCalculator.CrearPeriodo(Referencia, Referencia.AddDays(1));
        var pedidos = new[]
        {
            Pedido(1, 100m, PedidoEstado.ENTREGADO),
            Pedido(2, 200m, PedidoEstado.ENTREGADO),
            Pedido(3, 500m, PedidoEstado.CANCELADO),
        };

        var embudo = EstadisticasCalculator.EmbudoEstados(pedidos, periodo);

        embudo.Estados.Should().HaveCount(2);
        embudo.Cancelados.Pedidos.Should().Be(1);
        embudo.Cancelados.ImporteNoFacturado.Should().Be(500m);
        embudo.Totales.Recaudacion.Should().Be(300m, "los cancelados nunca suman");
        embudo.Totales.Pedidos.Should().Be(2);
    }

    [Test]
    public void CalcularComparativa_DevuelveNullSinPeriodoAnterior()
    {
        var actual = new TotalesDto(2, 4, 40m, 20m);
        var vacio = new TotalesDto(0, 0, 0m, 0m);

        EstadisticasCalculator.CalcularComparativa(actual, vacio, 2, 0).Should().BeNull();
    }

    [Test]
    public void CalcularComparativa_CalculaLosPorcentajesRespectoDelPeriodoAnterior()
    {
        var actual = new TotalesDto(4, 8, 200m, 50m);
        var anterior = new TotalesDto(2, 4, 100m, 50m);

        var comparativa = EstadisticasCalculator.CalcularComparativa(actual, anterior, 4, 2);

        comparativa.Should().NotBeNull();
        comparativa!.Recaudacion.Should().Be(100m, "200 frente a 100");
        comparativa.Pedidos.Should().Be(100m);
        comparativa.Unidades.Should().Be(100m);
        comparativa.Clientes.Should().Be(100m);
    }

    [Test]
    public void Inventario_CalculaValorAgotadosYStockBajo()
    {
        var productos = new Dictionary<long, Producto>
        {
            [1] = Producto(1, categoriaId: 1, precio: 10m, stock: 3),   // stock bajo
            [2] = Producto(2, categoriaId: 1, precio: 20m, stock: 0),   // agotado
            [3] = Producto(3, categoriaId: 2, precio: 5m, stock: 100),  // normal
        };
        var categorias = new Dictionary<long, Categoria>
        {
            [1] = new() { Id = 1, Nombre = "A" },
            [2] = new() { Id = 2, Nombre = "B" },
        };

        var inventario = EstadisticasCalculator.Inventario(productos, categorias, stockMax: 5);

        inventario.Productos.Total.Should().Be(3);
        inventario.Productos.SinStock.Should().Be(1);
        inventario.Productos.StockBajo.Should().Be(1, "el 0 no es stock bajo, es agotado");
        inventario.Productos.StockTotal.Should().Be(103);
        inventario.Productos.ValorInventario.Should().Be(30m + 0m + 500m);
        inventario.Agotados.Should().ContainSingle().Which.ProductoId.Should().Be(2);
        inventario.PorCategoria.Should().HaveCount(2);
        inventario.MayorValor[0].ProductoId.Should().Be(3, "5 × 100 = 500");
    }

    [Test]
    public void Precios_CalculaMinimoMaximoMediaYVariacion()
    {
        var puntos = new List<PuntoPrecioDto>
        {
            new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 10m),
            new(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), 20m),
            new(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), 30m),
        };

        var resultado = EstadisticasCalculator.Precios(5, puntos, null, null);

        resultado.Puntos.Should().Be(3);
        resultado.Minimo.Should().Be(10m);
        resultado.Maximo.Should().Be(30m);
        resultado.Media.Should().Be(20m);
        resultado.Ultimo.Should().Be(30m);
        resultado.VariacionAbsoluta.Should().Be(20m, "30 menos 10");
        resultado.VariacionPorcentaje.Should().Be(200m, "20 de ganancia sobre 10");
        resultado.Serie.Should().BeInAscendingOrder(p => p.Fecha, "la serie se devuelve ascendente");
    }

    [Test]
    public void Precios_SinSerie_DevuelveCeros()
    {
        var resultado = EstadisticasCalculator.Precios(5, [], null, null);

        resultado.Puntos.Should().Be(0);
        resultado.Maximo.Should().Be(0m);
        resultado.VariacionPorcentaje.Should().Be(0m);
    }
}
