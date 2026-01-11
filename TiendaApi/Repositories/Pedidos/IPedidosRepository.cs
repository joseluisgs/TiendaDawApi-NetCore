using TiendaApi.Models;

namespace TiendaApi.Repositories.Pedidos;

/// <summary>
/// Repository interface for Pedidos (MongoDB)
/// </summary>
public interface IPedidosRepository
{
    Task<IEnumerable<Pedido>> FindAllAsync();
    Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId);
    Task<Pedido?> FindByIdAsync(string id);
    Task<Pedido> SaveAsync(Pedido pedido);
    Task<Pedido> UpdateAsync(Pedido pedido);
}
