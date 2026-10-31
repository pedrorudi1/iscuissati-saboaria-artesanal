using Iscuissatis.Application.Payments;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Payments;

public sealed class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> FindByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken = default)
        => dbContext.Payments.FirstOrDefaultAsync(
            payment => payment.ProviderPaymentId == providerPaymentId, cancellationToken);

    public Task<bool> WebhookAlreadyProcessedAsync(string provider, string eventId, CancellationToken cancellationToken = default)
        => dbContext.ProcessedWebhookEvents.AnyAsync(
            item => item.Provider == provider && item.EventId == eventId, cancellationToken);

    public Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => dbContext.Payments
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .FirstOrDefaultAsync(payment => payment.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<Payment>> ListExpiredPendingAsync(
        DateTimeOffset now,
        int take = 50,
        CancellationToken cancellationToken = default)
        => await dbContext.Payments
            .Where(payment => payment.Status == Domain.Sales.PaymentStatus.Pending
                              && payment.ExpiresAtUtc != null
                              && payment.ExpiresAtUtc < now)
            .OrderBy(payment => payment.ExpiresAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(Payment payment) => dbContext.Payments.Add(payment);

    public void AddProcessedEvent(string provider, string eventId, DateTimeOffset receivedAtUtc)
        => dbContext.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent(provider, eventId, receivedAtUtc));

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
