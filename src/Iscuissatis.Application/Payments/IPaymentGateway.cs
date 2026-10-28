using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Payments;

public enum PaymentChargeStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

public sealed record PaymentCharge(
    string ProviderPaymentId,
    PaymentChargeStatus Status,
    string? CopyPaste,
    string? CheckoutUrl,
    DateTimeOffset? ExpiresAtUtc);

public enum WebhookOutcome
{
    Ignored = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Refunded = 4,
}

public sealed record PaymentWebhook(
    WebhookOutcome Outcome,
    string ProviderPaymentId,
    string EventId,
    string? OrderNumber,
    decimal? Amount,
    string RawStatus);

/// <summary>
/// Contrato de gateway. Cada provedor implementa a própria assinatura dentro de
/// ParseWebhook; o restante do sistema não conhece detalhes de provedor.
/// </summary>
public interface IPaymentGateway
{
    string Provider { get; }

    Task<PaymentCharge> CreateChargeAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Devolve null quando a assinatura é inválida — nunca lança por payload malformado.</summary>
    PaymentWebhook? ParseWebhook(string body, IReadOnlyDictionary<string, string> headers);
}

public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>Provedor ativo. "fake" existe para desenvolvimento e testes.</summary>
    public string Provider { get; set; } = "fake";

    public string WebhookSecret { get; set; } = "dev-webhook-secret";

    public int PixExpirationMinutes { get; set; } = 30;
}
