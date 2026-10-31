using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Iscuissatis.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Iscuissatis.Infrastructure.Integrations;

/// <summary>
/// Envio real pelo Resend (https://resend.com). A chave vem de variável de ambiente
/// (Email__ResendApiKey). Falha de envio vira exceção — quem chama decide se
/// deve interromper o fluxo (o notificador de pedidos NÃO interrompe).
/// </summary>
public sealed class ResendEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ResendApiKey))
        {
            throw new InvalidOperationException(
                "Email:Provider=resend exige Email__ResendApiKey configurada.");
        }

        var payload = new ResendRequest(
            _options.From,
            [message.To],
            message.Subject,
            message.HtmlBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ResendApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Resend recusou o envio para {To} com {Status}: {Body}",
                message.To, (int)response.StatusCode, body);

            throw new InvalidOperationException(
                $"Falha ao enviar e-mail pelo Resend ({(int)response.StatusCode}).");
        }

        logger.LogInformation("E-mail enviado pelo Resend para {To}: {Subject}", message.To, message.Subject);
    }

    private sealed record ResendRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);
}
