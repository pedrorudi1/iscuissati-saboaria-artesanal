using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Notifications;

public enum OrderEmailKind
{
    Confirmation = 0,
    Canceled = 1,
    Paid = 2,
    Shipped = 3,
    Delivered = 4,
}

/// <summary>
/// Notificações do pedido. Cada tipo é enviado uma única vez por pedido;
/// a garantia fica na implementação, com registro no banco.
/// </summary>
public interface IOrderEmailNotifier
{
    Task NotifyAsync(Order order, OrderEmailKind kind, CancellationToken cancellationToken = default);
}
