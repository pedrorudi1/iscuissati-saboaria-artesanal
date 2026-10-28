using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Sales;

public enum PaymentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Refunded = 3,
    Expired = 4,
}

/// <summary>
/// Cobrança gerada para um pedido. O identificador do provedor é a chave que
/// liga o webhook ao pedido — nunca confiamos em valor ou pedido vindos do corpo.
/// </summary>
public sealed class Payment
{
    private Payment()
    {
    }

    private Payment(
        Guid orderId,
        string provider,
        string providerPaymentId,
        decimal amount,
        string? copyPaste,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Provider = provider;
        ProviderPaymentId = providerPaymentId;
        Amount = amount;
        CopyPaste = copyPaste;
        ExpiresAtUtc = expiresAtUtc;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string ProviderPaymentId { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string? CopyPaste { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? SettledAtUtc { get; private set; }

    public static Payment Create(
        Guid orderId,
        string provider,
        string providerPaymentId,
        decimal amount,
        string? copyPaste,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset now)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Pagamento precisa estar ligado a um pedido.");
        }

        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            throw new DomainException("O provedor precisa devolver o identificador da cobrança.");
        }

        return new Payment(orderId, provider.Trim().ToLowerInvariant(), providerPaymentId.Trim(), amount, copyPaste, expiresAtUtc, now);
    }

    public void Approve(DateTimeOffset now)
    {
        if (Status == PaymentStatus.Approved)
        {
            return;
        }

        if (Status is PaymentStatus.Refunded or PaymentStatus.Expired)
        {
            throw new DomainException($"Não é possível aprovar um pagamento {Status}.");
        }

        Status = PaymentStatus.Approved;
        SettledAtUtc = now;
    }

    public void Reject(DateTimeOffset now)
    {
        if (Status == PaymentStatus.Approved)
        {
            return; // webhook fora de ordem não regride
        }

        Status = PaymentStatus.Rejected;
        SettledAtUtc = now;
    }

    public void Refund(DateTimeOffset now)
    {
        Status = PaymentStatus.Refunded;
        SettledAtUtc = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status != PaymentStatus.Pending)
        {
            return;
        }

        Status = PaymentStatus.Expired;
        SettledAtUtc = now;
    }
}
