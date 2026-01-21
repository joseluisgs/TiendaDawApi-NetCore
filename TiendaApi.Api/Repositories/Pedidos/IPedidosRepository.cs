using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Pedidos;

/// <summary>
/// Contrato del repositorio de pedidos.
/// </summary>
public interface IPedidosRepository
{
    /// <summary>Obtiene todos los pedidos ordenados por fecha.</summary>
    /// <returns>Colección de pedidos.</returns>
    Task<IEnumerable<Pedido>> FindAllAsync();

    /// <summary>Obtiene pedidos por ID de usuario.</summary>
    /// <param name="userId">ID del usuario.</param>
    /// <returns>Colección de pedidos.</returns>
    Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId);

    /// <summary>Obtiene pedidos paginados por usuario.</summary>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <returns>Tupla con items y total.</returns>
    Task<(IEnumerable<Pedido> Items, int TotalCount)> FindByUserIdPagedAsync(long userId, int page, int size);

    /// <summary>Busca un pedido por ID.</summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>Pedido o null.</returns>
    Task<Pedido?> FindByIdAsync(string id);

    /// <summary>Guarda un nuevo pedido.</summary>
    /// <param name="pedido">Pedido a guardar.</param>
    /// <returns>Pedido guardado.</returns>
    Task<Pedido> SaveAsync(Pedido pedido);

    /// <summary>Actualiza un pedido existente.</summary>
    /// <param name="pedido">Pedido con datos actualizados.</param>
    /// <returns>Pedido actualizado.</returns>
    Task<Pedido> UpdateAsync(Pedido pedido);
}
