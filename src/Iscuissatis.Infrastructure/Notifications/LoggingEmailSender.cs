using Iscuissatis.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace Iscuissatis.Infrastructure.Notifications;

/// <summary>
/// Sender de desenvolvimento: registra destinatário e assunto e não envia nada.
/// Trocar por SMTP/Resend é só registrar outra implementação de IEmailSender.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[email] para {To} | {Subject} | {Length} caracteres",
            message.To, message.Subject, message.HtmlBody.Length);

        return Task.CompletedTask;
    }
}
