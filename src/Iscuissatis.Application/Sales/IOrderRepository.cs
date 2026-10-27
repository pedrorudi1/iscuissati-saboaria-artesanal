using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Sales;

public sealed record StockRequest(Guid VariantId, string ProductName, int Quantity);

public interface IOrderRepository
{
    /// <summary>
    /// Grava o pedido e reserva o estoque numa única transação.
    /// Devolve null quando tudo foi reservado, ou a mensagem do item que faltou estoque.
    /// </summary>
    Task<string?> CreateWithReservationAsync(
        Order order,
        IReadOnlyList<StockRequest> requests,
        IReadOnlyList<StockReservation> reservations,
        CancellationToken cancellationToken = default);

    Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> FindByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Libera as reservas ativas do pedido e devolve o estoque.</summary>
    Task ReleaseReservationsAsync(Guid orderId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
