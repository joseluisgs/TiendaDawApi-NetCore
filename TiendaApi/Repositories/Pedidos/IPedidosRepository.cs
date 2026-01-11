using TiendaApi.Models;

namespace TiendaApi.Repositories.Pedidos;

/// <summary>
/// Interfaz del repositorio de pedidos.
/// </summary>
public interface IPedidosRepository
{
    /// <summary>
    /// Obtiene todos los pedidos ordenados por fecha de creación descendente.
    /// </summary>
    /// <returns>Colección de todos los pedidos.</returns>
    Task<IEnumerable<Pedido>> FindAllAsync();

    /// <summary>
    /// Obtiene pedidos por identificador de usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Colección de pedidos del usuario.</returns>
    Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId);

    /// <summary>
    /// Obtiene un pedido por su identificador.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <returns>El pedido encontrado o null si no existe.</returns>
    Task<Pedido?> FindByIdAsync(string id);

    /// <summary>
    /// Guarda un nuevo pedido.
    /// </summary>
    /// <param name="pedido">Pedido a guardar.</param>
    /// <returns>El pedido guardado con los datos actualizados.</returns>
    Task<Pedido> SaveAsync(Pedido pedido);

    /// <summary>
    /// Actualiza un pedido existente.
    /// </summary>
    /// <param name="pedido">Pedido con los datos actualizados.</param>
    /// <returns>El pedido actualizado.</returns>
    Task<Pedido> UpdateAsync(Pedido pedido);
}
