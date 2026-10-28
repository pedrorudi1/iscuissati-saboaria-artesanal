using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Common;

namespace Iscuissatis.Application.Payments;

/// <summary>
/// Cancela pedidos cujo Pix não foi pago dentro da janela e devolve o estoque.
/// Roda em segundo plano; também pode ser chamado manualmente (testes, admin).
/// </summary>
public sealed class PaymentExpirationService(
    IPaymentRepository payments,
    OrderService orders,
    TimeProvider clock)
{
    public async Task<int> ExpireOverdueAsync(
        DateTimeOffset? asOf = null,
        CancellationToken cancellationToken = default)
    {
        var now = asOf ?? clock.GetUtcNow();
        var overdue = await payments.ListExpiredPendingAsync(now, 50, cancellationToken);
        var expired = 0;

        foreach (var payment in overdue)
        {
            try
            {
                await orders.CancelAsync(payment.OrderId, cancellationToken);
                payment.Expire(now);
                expired++;
            }
            catch (DomainException)
            {
                // Já cancelado, pago ou despachado: só encerra a cobrança.
                payment.Expire(now);
            }
        }

        if (overdue.Count > 0)
        {
            await payments.SaveChangesAsync(cancellationToken);
        }

        return expired;
    }
}
