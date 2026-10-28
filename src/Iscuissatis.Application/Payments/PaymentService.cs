using Iscuissatis.Application.Notifications;
using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Payments;

public enum WebhookResult
{
    Processed = 0,
    Duplicate = 1,
    InvalidSignature = 2,
    UnknownProvider = 3,
    NotFound = 4,
}

/// <summary>
/// Orquestra cobrança e webhook. Este é o ÚNICO caminho que leva um pedido
/// para Paid — nem tela de retorno nem ação de navegador conseguem isso.
/// </summary>
public sealed class PaymentService(
    IPaymentGateway gateway,
    IPaymentRepository payments,
    IOrderRepository orders,
    IOrderEmailNotifier emails,
    TimeProvider clock)
{
    public async Task<Payment> StartAsync(Order order, CancellationToken cancellationToken = default)
    {
        var charge = await gateway.CreateChargeAsync(order, cancellationToken);

        var payment = Payment.Create(
            order.Id, gateway.Provider, charge.ProviderPaymentId, order.Total,
            charge.CopyPaste, charge.ExpiresAtUtc, clock.GetUtcNow());

        payments.Add(payment);
        await payments.SaveChangesAsync(cancellationToken);

        return payment;
    }

    public async Task<WebhookResult> HandleWebhookAsync(
        string provider,
        string body,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(provider, gateway.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return WebhookResult.UnknownProvider;
        }

        var webhook = gateway.ParseWebhook(body, headers);

        if (webhook is null)
        {
            return WebhookResult.InvalidSignature;
        }

        if (await payments.WebhookAlreadyProcessedAsync(provider, webhook.EventId, cancellationToken))
        {
            return WebhookResult.Duplicate;
        }

        var payment = await payments.FindByProviderPaymentIdAsync(webhook.ProviderPaymentId, cancellationToken);

        if (payment is null)
        {
            return WebhookResult.NotFound;
        }

        var order = await orders.FindAsync(payment.OrderId, cancellationToken);

        if (order is null)
        {
            return WebhookResult.NotFound;
        }

        var now = clock.GetUtcNow();

        switch (webhook.Outcome)
        {
            case WebhookOutcome.Approved:
                payment.Approve(now);

                if (order.Status == OrderStatus.PendingPayment)
                {
                    order.MarkAsPaid(now);
                    await emails.NotifyAsync(order, OrderEmailKind.Paid, cancellationToken);
                }

                break;

            case WebhookOutcome.Rejected:
                payment.Reject(now); // fora de ordem não regride pedido já pago
                break;

            case WebhookOutcome.Refunded:
                payment.Refund(now);

                if (order.Status is OrderStatus.Paid or OrderStatus.Preparing or OrderStatus.Shipped or OrderStatus.Delivered)
                {
                    order.Refund(now);
                }

                break;
        }

        payments.AddProcessedEvent(provider, webhook.EventId, now);
        await payments.SaveChangesAsync(cancellationToken);

        return WebhookResult.Processed;
    }
}
