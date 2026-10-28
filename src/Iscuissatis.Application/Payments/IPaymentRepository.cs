using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Payments;

public interface IPaymentRepository
{
    Task<Payment?> FindByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken = default);

    Task<bool> WebhookAlreadyProcessedAsync(string provider, string eventId, CancellationToken cancellationToken = default);

    void Add(Payment payment);

    void AddProcessedEvent(string provider, string eventId, DateTimeOffset receivedAtUtc);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
