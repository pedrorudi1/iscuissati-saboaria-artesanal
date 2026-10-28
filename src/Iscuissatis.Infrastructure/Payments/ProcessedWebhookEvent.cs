namespace Iscuissatis.Infrastructure.Payments;

/// <summary>
/// Registro de evento já processado. O índice único (Provider, EventId) é o que
/// garante idempotência mesmo com reenvios do provedor.
/// </summary>
public sealed class ProcessedWebhookEvent
{
    private ProcessedWebhookEvent()
    {
    }

    public ProcessedWebhookEvent(string provider, string eventId, DateTimeOffset receivedAtUtc)
    {
        Id = Guid.NewGuid();
        Provider = provider;
        EventId = eventId;
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string EventId { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedAtUtc { get; private set; }
}
