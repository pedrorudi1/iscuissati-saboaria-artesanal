using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Payments;

public interface IPaymentRepository
{
    Task<Payment?> FindByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken = default);

    Task<bool> WebhookAlreadyProcessedAsync(string provider, string eventId, CancellationToken cancellationToken = default);

    Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> ListExpiredPendingAsync(
        DateTimeOffset now,
        int take = 50,
        CancellationToken cancellationToken = default);

    void Add(Payment payment);

    void AddProcessedEvent(string provider, string eventId, DateTimeOffset receivedAtUtc);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
