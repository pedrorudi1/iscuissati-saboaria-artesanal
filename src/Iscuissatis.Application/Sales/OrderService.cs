using Iscuissatis.Application.Shipping;
using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Sales;

public sealed class OrderService(IOrderRepository orders, TimeProvider clock)
{
    /// <summary>Reserva de estoque para Pix: 30 minutos, conforme a spec 04.</summary>
    public static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(30);

    public async Task<Order> PlaceOrderAsync(
        Cart cart,
        Guid? customerId,
        string? guestEmail,
        ShippingAddress address,
        ShippingOption? shipping = null,
        CancellationToken cancellationToken = default)
    {
        if (cart.Items.Count == 0)
        {
            throw new DomainException("Sua sacola está vazia.");
        }

        var now = clock.GetUtcNow();
        var number = BuildOrderNumber(now);

        var items = cart.Items
            .Select(item => (item.ProductVariantId, item.ProductName, item.VariantName, item.UnitPrice, item.Quantity))
            .ToList();

        var order = Order.Create(number, customerId, guestEmail, address, items, now);

        if (shipping is not null)
        {
            order.SetShipping(shipping.Price, now);
        }

        var requests = cart.Items
            .Select(item => new StockRequest(item.ProductVariantId, item.ProductName, item.Quantity))
            .ToList();

        var reservations = cart.Items
            .Select(item => new StockReservation(order.Id, item.ProductVariantId, item.Quantity, now.Add(ReservationWindow)))
            .ToList();

        var error = await orders.CreateWithReservationAsync(order, requests, reservations, cancellationToken);

        if (error is not null)
        {
            throw new DomainException(error);
        }

        return order;
    }

    public Task<Order?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
        => orders.FindByNumberAsync(number, cancellationToken);

    public async Task<Order> CancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orders.FindAsync(orderId, cancellationToken)
            ?? throw new DomainException("Pedido não encontrado.");

        order.Cancel(clock.GetUtcNow());

        await orders.ReleaseReservationsAsync(order.Id, clock.GetUtcNow(), cancellationToken);
        await orders.SaveChangesAsync(cancellationToken);

        return order;
    }

    private static string BuildOrderNumber(DateTimeOffset now)
        => $"{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
