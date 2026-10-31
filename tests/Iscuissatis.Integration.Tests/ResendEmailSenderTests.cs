using System.Net;
using System.Text;
using Iscuissatis.Application.Notifications;
using Iscuissatis.Infrastructure.Integrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

public class ResendEmailSenderTests
{
    private sealed class CapturingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent("{\"id\":\"abc\"}", Encoding.UTF8, "application/json"),
            };
        }
    }

    private static ResendEmailSender Sender(CapturingHandler handler, string apiKey = "re_test_123")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var options = Options.Create(new EmailOptions
        {
            Provider = "resend",
            From = "Iscuissati's <no-reply@loja.test>",
            ResendApiKey = apiKey,
        });

        return new ResendEmailSender(client, options, NullLogger<ResendEmailSender>.Instance);
    }

    [Fact]
    public async Task Sends_the_expected_payload_with_bearer_token()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);
        var sender = Sender(handler);

        await sender.SendAsync(new EmailMessage("cliente@teste.local", "Pedido 123", "<p>ok</p>"));

        handler.Request!.Method.ShouldBe(HttpMethod.Post);
        handler.Request.RequestUri!.ToString().ShouldBe("https://api.resend.com/emails");
        handler.Request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        handler.Request.Headers.Authorization.Parameter.ShouldBe("re_test_123");

        handler.Body.ShouldNotBeNull();
        handler.Body!.ShouldContain("cliente@teste.local");
        handler.Body.ShouldContain("Pedido 123");
        handler.Body.ShouldContain("no-reply@loja.test");
    }

    [Fact]
    public async Task Throws_when_the_provider_rejects()
    {
        var handler = new CapturingHandler(HttpStatusCode.UnprocessableEntity);
        var sender = Sender(handler);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            sender.SendAsync(new EmailMessage("cliente@teste.local", "Pedido 123", "<p>ok</p>")));
    }

    [Fact]
    public async Task Refuses_to_send_without_api_key()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);
        var sender = Sender(handler, apiKey: "  ");

        await Should.ThrowAsync<InvalidOperationException>(() =>
            sender.SendAsync(new EmailMessage("cliente@teste.local", "Pedido 123", "<p>ok</p>")));
    }
}
