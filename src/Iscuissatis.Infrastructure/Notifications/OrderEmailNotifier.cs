using Iscuissatis.Application.Notifications;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Iscuissatis.Infrastructure.Notifications;

public sealed class OrderEmailNotifier(
    AppDbContext dbContext,
    IEmailSender emailSender,
    TimeProvider clock,
    ILogger<OrderEmailNotifier> logger) : IOrderEmailNotifier
{
    public async Task NotifyAsync(Order order, OrderEmailKind kind, CancellationToken cancellationToken = default)
    {
        var to = order.GuestEmail;

        if (string.IsNullOrWhiteSpace(to))
        {
            logger.LogInformation("Pedido {Number} sem e-mail de contato; notificação {Kind} ignorada.", order.Number, kind);
            return;
        }

        var alreadySent = await dbContext.SentOrderEmails
            .AnyAsync(item => item.OrderId == order.Id && item.Kind == kind, cancellationToken);

        if (alreadySent)
        {
            return;
        }

        try
        {
            await emailSender.SendAsync(BuildMessage(order, kind, to), cancellationToken);
        }
        catch (Exception exception)
        {
            // E-mail é aviso, não regra de negócio: a compra não pode falhar por causa dele.
            // Como não registramos o envio, uma retentativa futura ainda é possível.
            logger.LogError(exception, "Falha ao enviar {Kind} do pedido {Number}.", kind, order.Number);
            return;
        }

        dbContext.SentOrderEmails.Add(new SentOrderEmail(order.Id, kind, to, clock.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static EmailMessage BuildMessage(Order order, OrderEmailKind kind, string to)
    {
        var (subject, headline) = kind switch
        {
            OrderEmailKind.Confirmation => ($"Pedido {order.Number} recebido", "Recebemos seu pedido"),
            OrderEmailKind.Canceled => ($"Pedido {order.Number} cancelado", "Seu pedido foi cancelado"),
            OrderEmailKind.Paid => ($"Pagamento confirmado — {order.Number}", "Pagamento confirmado"),
            OrderEmailKind.Shipped => ($"Pedido {order.Number} enviado", "Seu pedido saiu"),
            _ => ($"Pedido {order.Number} entregue", "Pedido entregue"),
        };

        var items = string.Join("", order.Items.Select(item =>
            $"<tr><td style=\"padding:8px 0\">{item.Quantity} × {item.ProductName} ({item.VariantName})</td>" +
            $"<td align=\"right\">R$ {item.LineTotal:N2}</td></tr>"));

        var html = $"""
            <div style="font-family:Lato,Arial,sans-serif;color:#3A2A20;background:#F3EAD8;padding:24px">
              <div style="max-width:560px;margin:0 auto;background:#FBF7EF;border:1px solid #D9C9AA;border-radius:8px;padding:28px">
                <p style="font-family:Oswald,Arial,sans-serif;text-transform:uppercase;letter-spacing:.08em;color:#A9482B;margin:0 0 4px">
                  Iscuissati's Saboaria Artesanal
                </p>
                <h1 style="font-family:Oswald,Arial,sans-serif;font-size:22px;margin:0 0 16px">{headline}</h1>
                <p style="margin:0 0 8px">Pedido <strong>{order.Number}</strong></p>
                <table style="width:100%;border-collapse:collapse;font-size:14px">{items}</table>
                <p style="margin:16px 0 0;font-size:14px">
                  Total: <strong>R$ {order.Total:N2}</strong>
                </p>
                <p style="margin:16px 0 0;font-size:12px;color:#5E4A2E">
                  Endereço: {order.ShippingAddress.Recipient}, {order.ShippingAddress.Street}, {order.ShippingAddress.Number} —
                  {order.ShippingAddress.City}/{order.ShippingAddress.State} — CEP {order.ShippingAddress.ZipCode}
                </p>
              </div>
            </div>
            """;

        return new EmailMessage(to, subject, html);
    }
}
