using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Pedidos;

/// <summary>
/// Define el contrato para el repositorio de pedidos.
/// 
/// El repositorio de pedidos gestiona la entidad Pedido y sus relaciones,
/// encapsulando todas las operaciones relacionadas con el proceso de pedidos
/// de la tienda.
/// 
/// Responsabilidades principales:
/// 
/// 1. **Gestión del ciclo de vida del pedido**: Creación, lectura y actualización
///    de pedidos durante todo su ciclo de vida (pendiente, confirmado, enviado, etc.).
/// 
/// 2. **Consulta por usuario**: Métodos específicos para obtener pedidos
///    filtrados por usuario, esenciales para el historial de compras.
/// 
/// 3. **Histórico de transacciones**: Los pedidos son registros transaccionales
///    que no se eliminan, solo se actualiza su estado.
/// 
/// 4. **Relaciones complejas**: Un pedido tiene múltiples líneas de detalle
///    (PedidoDetalles) y está vinculado a un usuario.
/// 
/// Patrón de uso:
/// Los pedidos siguen un flujo donde:
///
/// - Se crean con estado "Pendiente" o "EnProceso".
/// - El stock se decrementa al confirmar.
/// - El estado evoluciona según la logística (enviado, entregado).
/// - Las actualizaciones de estado son críticas para la trazabilidad.
/// 
/// Este repositorio NO incluye eliminación de pedidos; son registros permanentes
/// para cumplimiento legal y trazabilidad de negocio.
/// </summary>
public interface IPedidosRepository
{
    /// <summary>
    /// Recupera todos los pedidos del sistema ordenados por fecha descendente.
    /// 
    /// <remarks>
    /// Este método carga todos los pedidos en memoria. Para sistemas con alto
    /// volumen de pedidos, use paginación o filtros específicos.
    /// 
    /// El orden por fecha descendente (más recientes primero) es ideal para:
    /// - Paneles de administración.
    /// - Reportes de actividad reciente.
    /// - Auditorías de últimos movimientos.
    /// 
    /// Los detalles del pedido (líneas) no se cargan automáticamente.
    /// Use Include() en el DbContext si los necesita.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Panel de administración - pedidos recientes
    /// var pedidos = await _pedidosRepository.FindAllAsync();
    /// var pedidosRecientes = pedidos.Take(10);
    /// </code>
    /// </example>
    /// 
    /// <returns>Colección de todos los pedidos ordenados por fecha de creación descendente.</returns>
    Task<IEnumerable<Pedido>> FindAllAsync();

    /// <summary>
    /// Recupera todos los pedidos de un usuario específico.
    /// 
    /// <remarks>
    /// Este método es esencial para el historial de compras del usuario.
    /// Retorna todos los pedidos independientemente de su estado.
    /// 
    /// El resultado está ordenado por fecha descendente para mostrar
    /// primero los pedidos más recientes del usuario.
    /// 
    /// Útil para:
    /// - Mostrar historial de compras en el perfil del usuario.
    /// - Sección "Mis pedidos" de la aplicación.
    /// - Consultas de estado de pedidos por el cliente.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Historial de compras del usuario
    /// var pedidos = await _pedidosRepository.FindByUserIdAsync(userId);
    /// foreach (var pedido in pedidos)
    /// {
    ///     Console.WriteLine($"Pedido #{pedido.Id} - {pedido.Estado}");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="userId">Identificador del usuario cuyos pedidos se quieren obtener.</param>
    /// <returns>Colección de pedidos del usuario ordenados por fecha.</returns>
    Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId);

    /// <summary>
    /// Recupera pedidos de un usuario de forma paginada.
    /// 
    /// <remarks>
    /// Este método combina el filtrado por usuario con paginación, ideal para
    /// aplicaciones con usuarios que tienen muchos pedidos históricos.
    /// 
    /// La paginación permite cargar el historial de forma eficiente sin
    /// sobrecargar la memoria con todos los pedidos del usuario.
    /// 
    /// Parámetros de paginación:
    /// - page: Número de página (0-based). Primera página es 0.
    /// - size: Cantidad de pedidos por página.
    /// 
    /// El retorno incluye el total para calcular cuántas páginas hay.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Historial paginado - página 1 (segunda página)
    /// const int PAGE_SIZE = 10;
    /// var (pedidos, total) = await _pedidosRepository.FindByUserIdPagedAsync(
    ///     userId, page: 1, size: PAGE_SIZE);
    /// 
    /// var totalPages = (int)Math.Ceiling(total / (double)PAGE_SIZE);
    /// </code>
    /// </example>
    /// 
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="page">Número de página (0-based).</param>
    /// <param name="size">Cantidad de pedidos por página.</param>
    /// <returns>Tupla con pedidos de la página y total de pedidos del usuario.</returns>
    Task<(IEnumerable<Pedido> Items, int TotalCount)> FindByUserIdPagedAsync(long userId, int page, int size);

    /// <summary>
    /// Busca un pedido específico por su identificador único.
    /// 
    /// <remarks>
    /// Recupera un pedido individual por su ID. Este método es fundamental para:
    /// 
    /// - Visualización de detalles de pedido.
    /// - Gestión de estados por administradores.
    /// - Consultas de trazabilidad.
    /// 
    /// El ID del pedido es típicamente un string (puede incluir prefix o código
    /// amigable) a diferencia de otros repositorios que usan long.
    /// 
    /// Si el pedido tiene líneas de detalle, estas no se cargan automáticamente.
    /// Use Include() en el DbContext si las necesita para mostrar el detalle completo.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Ver detalles de un pedido
    /// var pedido = await _pedidosRepository.FindByIdAsync(pedidoId);
    /// if (pedido == null)
    /// {
    ///     return NotFound("Pedido no encontrado");
    /// }
    /// 
    /// Console.WriteLine($"Pedido #{pedido.Id}");
    /// Console.WriteLine($"Estado: {pedido.Estado}");
    /// Console.WriteLine($"Total: ${pedido.Total}");
    /// </code>
    /// </example>
    /// 
    /// <param name="id">Identificador del pedido (string).</param>
    /// <returns>El pedido encontrado o null si no existe.</returns>
    Task<Pedido?> FindByIdAsync(string id);

    /// <summary>
    /// Persiste un nuevo pedido en la base de datos.
    /// 
    /// <remarks>
    /// Crea un nuevo registro de pedido con sus líneas de detalle.
    /// Este es el método principal para el flujo de compra.
    /// 
    /// El pedido creado típicamente tiene:
    /// - Estado inicial (Pendiente, EnProceso, etc.).
    /// - Fecha de creación establecida.
    /// - Total calculado basado en las líneas.
    /// - Relación con el usuario que realizó el pedido.
    /// 
    /// Proceso típico de creación de pedido:
    /// 1. Crear objeto Pedido con datos básicos.
    /// 2. Agregar líneas de detalle (PedidoDetalle).
    /// 3. Calcular total.
    /// 4. (Opcional) Disminuir stock de productos.
    /// 5. Llamar a SaveAsync.
    /// 
    /// Nota: Para decrementar stock de forma atómica con el pedido,
    /// use una transacción explícita que involucre también el IProductoRepository.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Crear nuevo pedido
    /// var pedido = new Pedido
    /// {
    ///     UserId = userId,
    ///     Estado = PedidoEstado.Pendiente,
    ///     DireccionEnvio = "Calle Principal 123",
    ///     Detalles = new List&lt;PedidoDetalle&gt;
    ///     {
    ///         new PedidoDetalle { ProductoId = 1, Cantidad = 2, PrecioUnitario = 99.99m },
    ///         new PedidoDetalle { ProductoId = 5, Cantidad = 1, PrecioUnitario = 49.99m }
    ///     }
    /// };
    /// 
    /// pedido.CalcularTotal();
    /// var pedidoGuardado = await _pedidosRepository.SaveAsync(pedido);
    /// </code>
    /// </example>
    /// 
    /// <param name="pedido">Pedido a persistir, incluyendo sus líneas de detalle.</param>
    /// <returns>El pedido guardado con datos actualizados (ID, fecha, etc.).</returns>
    Task<Pedido> SaveAsync(Pedido pedido);

    /// <summary>
    /// Actualiza un pedido existente.
    /// 
    /// <remarks>
    /// Actualiza los datos de un pedido. Los casos de uso principales son:
    /// 
    /// 1. **Cambio de estado**: El estado del pedido evoluciona durante el proceso:
    ///    Pendiente → Confirmado → Enviado → Entregado → Completado
    ///    Cancelado → Anulado
    /// 
    /// 2. **Actualización de datos**: Dirección de envío, información de contacto,
    ///    notas internas, etc.
    /// 
    /// Consideraciones:
    /// - No modifique los detalles del pedido después de guardado.
    /// - Para cancelar, es preferible crear un nuevo pedido y anular este.
    /// - Los cambios de estado deben registrar quién los hizo y cuándo (auditoría).
    /// - Ciertos estados (como "Enviado") no deberían cambiarse a estados anteriores
    ///   sin validación de negocio.
    /// 
    /// El repositorio no valida transiciones de estado; esto es responsabilidad
    /// del servicio de pedidos.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Actualizar estado de pedido
    /// var pedido = await _pedidosRepository.FindByIdAsync(pedidoId);
    /// pedido.Estado = PedidoEstado.Enviado;
    /// pedido.NumeroSeguimiento = "TRACK123456";
    /// pedido.FechaEnvio = DateTime.UtcNow;
    /// 
    /// await _pedidosRepository.UpdateAsync(pedido);
    /// </code>
    /// </example>
    /// 
    /// <param name="pedido">Pedido con datos actualizados.</param>
    /// <returns>El pedido actualizado.</returns>
    Task<Pedido> UpdateAsync(Pedido pedido);
}
