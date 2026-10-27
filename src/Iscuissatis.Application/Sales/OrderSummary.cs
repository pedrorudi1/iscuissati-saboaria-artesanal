using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Sales;

public sealed record OrderSummary(
    Guid Id,
    string Number,
    string? CustomerEmail,
    string Recipient,
    OrderStatus Status,
    decimal Total,
    int ItemCount,
    DateTimeOffset CreatedAtUtc);
