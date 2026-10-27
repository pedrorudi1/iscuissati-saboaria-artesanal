using Iscuissatis.Application.Notifications;

namespace Iscuissatis.Infrastructure.Notifications;

/// <summary>
/// Registro de e-mail já enviado. O índice único (OrderId, Kind) garante
/// "uma vez por mudança de status", mesmo se o fluxo for reprocessado.
/// </summary>
public sealed class SentOrderEmail
{
    private SentOrderEmail()
    {
    }

    public SentOrderEmail(Guid orderId, OrderEmailKind kind, string to, DateTimeOffset sentAtUtc)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Kind = kind;
        To = to;
        SentAtUtc = sentAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public OrderEmailKind Kind { get; private set; }

    public string To { get; private set; } = string.Empty;

    public DateTimeOffset SentAtUtc { get; private set; }
}
