using System.Text.Json;
using Iscuissatis.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

[AllowAnonymous]
[ApiController]
[Route("webhooks/pagamentos")]
public class WebhookController(PaymentService paymentService) : ControllerBase
{
    [HttpPost("{provider}")]
    public async Task<IActionResult> Receive(string provider, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var headers = Request.Headers
            .ToDictionary(header => header.Key, header => header.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var result = await paymentService.HandleWebhookAsync(provider, body, headers, cancellationToken);

        return result switch
        {
            WebhookResult.Processed => Ok(new { status = "processed" }),
            WebhookResult.Duplicate => Ok(new { status = "duplicate" }),
            WebhookResult.InvalidSignature => Unauthorized(new { error = "assinatura inválida" }),
            WebhookResult.UnknownProvider => NotFound(new { error = "provedor desconhecido" }),
            _ => NotFound(new { error = "cobrança não encontrada" }),
        };
    }
}
