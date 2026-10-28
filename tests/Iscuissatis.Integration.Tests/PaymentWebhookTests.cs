using System.Net;
using System.Security.Cryptography;
using System.Text;
using Iscuissatis.Application.Notifications;
using Iscuissatis.Application.Payments;
using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class PaymentWebhookTests(IntegrationTestFixture fixture)
{
    private const string Secret = "test-webhook-secret";

    [Fact]
    public async Task Valid_webhook_marks_the_order_as_paid_and_sends_one_email()
    {
        RecordingEmailSender.Reset();
        var (orderId, number, paymentId, email) = await CreateOrderWithChargeAsync();

        var body = Payload($"evt-{Guid.NewGuid():N}", paymentId, number, "approved");
        var response = await SendAsync(body, Sign(body));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("processed");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await db.Orders.Where(order => order.Id == orderId).Select(order => order.Status).SingleAsync())
            .ShouldBe(OrderStatus.Paid);
        (await db.Payments.Where(payment => payment.ProviderPaymentId == paymentId)
            .Select(payment => payment.Status).SingleAsync())
            .ShouldBe(PaymentStatus.Approved);

        RecordingEmailSender.Sent.Count(message => message.To == email && message.Subject.Contains("Pagamento")).ShouldBe(1);
    }

    [Fact]
    public async Task Repeated_webhook_does_not_duplicate_effects()
    {
        RecordingEmailSender.Reset();
        var (orderId, number, paymentId, email) = await CreateOrderWithChargeAsync();

        var eventId = $"evt-{Guid.NewGuid():N}";
        var body = Payload(eventId, paymentId, number, "approved");

        await SendAsync(body, Sign(body));
        var second = await SendAsync(body, Sign(body));

        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await second.Content.ReadAsStringAsync()).ShouldContain("duplicate");

        RecordingEmailSender.Sent.Count(message => message.To == email).ShouldBe(1);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.ProcessedWebhookEvents.CountAsync(item => item.EventId == eventId)).ShouldBe(1);

        _ = orderId;
    }

    [Fact]
    public async Task Invalid_signature_is_rejected_and_nothing_changes()
    {
        var (orderId, number, paymentId, _) = await CreateOrderWithChargeAsync();

        var body = Payload($"evt-{Guid.NewGuid():N}", paymentId, number, "approved");
        var response = await SendAsync(body, "assinatura-forjada");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Orders.Where(order => order.Id == orderId).Select(order => order.Status).SingleAsync())
            .ShouldBe(OrderStatus.PendingPayment);
    }

    [Fact]
    public async Task Late_pending_event_does_not_downgrade_a_paid_order()
    {
        var (orderId, number, paymentId, _) = await CreateOrderWithChargeAsync();

        var approved = Payload($"evt-{Guid.NewGuid():N}", paymentId, number, "approved");
        await SendAsync(approved, Sign(approved));

        var pending = Payload($"evt-{Guid.NewGuid():N}", paymentId, number, "pending");
        await SendAsync(pending, Sign(pending));

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Orders.Where(order => order.Id == orderId).Select(order => order.Status).SingleAsync())
            .ShouldBe(OrderStatus.Paid);
    }

    private async Task<HttpResponseMessage> SendAsync(string body, string signature)
    {
        var client = fixture.Factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/pagamentos/fake")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Signature", signature);

        return await client.SendAsync(request);
    }

    private static string Sign(string body)
        => Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

    private static string Payload(string eventId, string paymentId, string orderNumber, string status)
        => $"{{\"eventId\":\"{eventId}\",\"paymentId\":\"{paymentId}\",\"orderNumber\":\"{orderNumber}\",\"amount\":43.80,\"status\":\"{status}\"}}";

    private async Task<(Guid OrderId, string Number, string PaymentId, string Email)> CreateOrderWithChargeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var email = $"pagamento.{suffix}@teste.local";
        Guid variantId;

        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Pagamento {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create($"Sabonete Pagamento {suffix}", category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"PAG-{suffix}", 24.90m, 5, 100, DateTimeOffset.UtcNow);
            product.AddImage($"/media/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);
            product.Publish(DateTimeOffset.UtcNow);

            db.Products.Add(product);
            await db.SaveChangesAsync();
            variantId = variant.Id;
        }

        using var scope = fixture.Factory.Services.CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<CartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
        var paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();

        var cart = await cartService.AddItemAsync($"anon-{suffix}", variantId, 1);
        var order = await orderService.PlaceOrderAsync(
            cart, null, email,
            ShippingAddress.Create("Cliente", "Rua A", "1", null, "Centro", "Tatuí", "SP", "18300000"));

        var payment = await paymentService.StartAsync(order);

        return (order.Id, order.Number, payment.ProviderPaymentId, email);
    }
}
