namespace Iscuissatis.Application.Notifications;

public sealed record EmailMessage(string To, string Subject, string HtmlBody);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
