using System.Collections.Concurrent;
using Iscuissatis.Application.Notifications;

namespace Iscuissatis.Integration.Tests;

/// <summary>Captura os e-mails em memória para os testes conferirem o que seria enviado.</summary>
public sealed class RecordingEmailSender : IEmailSender
{
    public static readonly ConcurrentQueue<EmailMessage> Sent = new();

    public static void Reset() => Sent.Clear();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
