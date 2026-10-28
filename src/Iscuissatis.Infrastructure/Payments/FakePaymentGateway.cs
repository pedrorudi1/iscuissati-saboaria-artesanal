using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Iscuissatis.Application.Payments;
using Iscuissatis.Domain.Sales;
using Microsoft.Extensions.Options;

namespace Iscuissatis.Infrastructure.Payments;

/// <summary>
/// Provedor de desenvolvimento: gera um "Pix" fictício e valida webhook por
/// HMAC-SHA256 (header X-Signature). Serve para exercitar todo o fluxo —
/// contrato, idempotência e e-mail — sem depender de conta em gateway.
/// </summary>
public sealed class FakePaymentGateway(IOptions<PaymentOptions> options, TimeProvider clock) : IPaymentGateway
{
    private readonly PaymentOptions _options = options.Value;

    public string Provider => "fake";

    public Task<PaymentCharge> CreateChargeAsync(Order order, CancellationToken cancellationToken = default)
    {
        var id = $"fake_{Guid.NewGuid():N}";
        var expiresAt = clock.GetUtcNow().AddMinutes(_options.PixExpirationMinutes);

        var copyPaste = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"00020126PIX-FAKE|pedido={order.Number}|valor={order.Total:F2}|expira={expiresAt:O}"));

        return Task.FromResult(new PaymentCharge(id, PaymentChargeStatus.Pending, copyPaste, null, expiresAt));
    }

    public PaymentWebhook? ParseWebhook(string body, IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("X-Signature", out var signature) || string.IsNullOrWhiteSpace(signature))
        {
            return null;
        }

        var expected = Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(_options.WebhookSecret),
                Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        var provided = signature.Trim().ToLowerInvariant();

        if (provided.Length != expected.Length
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided),
                Encoding.UTF8.GetBytes(expected)))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var paymentId = root.TryGetProperty("paymentId", out var payment) ? payment.GetString() : null;
            var eventId = root.TryGetProperty("eventId", out var evt) ? evt.GetString() : null;
            var status = root.TryGetProperty("status", out var raw) ? raw.GetString() : null;
            var orderNumber = root.TryGetProperty("orderNumber", out var order) ? order.GetString() : null;
            decimal? amount = root.TryGetProperty("amount", out var value) && value.TryGetDecimal(out var parsed)
                ? parsed
                : null;

            if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            var outcome = status.Trim().ToLowerInvariant() switch
            {
                "approved" or "paid" => WebhookOutcome.Approved,
                "rejected" or "failed" => WebhookOutcome.Rejected,
                "refunded" => WebhookOutcome.Refunded,
                "pending" => WebhookOutcome.Pending,
                _ => WebhookOutcome.Ignored,
            };

            return new PaymentWebhook(outcome, paymentId!, eventId!, orderNumber, amount, status!);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
