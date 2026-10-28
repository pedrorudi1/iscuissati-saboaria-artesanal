using System.Net;
using System.Security.Cryptography;
using System.Text;
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
public class PixCheckoutTests(IntegrationTestFixture fixture)
{
    private const string Secret = "test-webhook-secret";

    [Fact]
    public async Task Confirmation_shows_pix_qr_and_copy_paste_while_pending()
    {
        var (number, _, _) = await CreateOrderAsync(stock: 3, quantity: 1);

        var client = fixture.Factory.CreateClient();
        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync($"/pedido/{number}")).Content.ReadAsStringAsync());

        html.ShouldContain("Pague com Pix");
        html.ShouldContain("data:image/png;base64,");
        html.ShouldContain("expira em");
    }

    [Fact]
    public async Task Expired_pix_cancels_the_order_and_returns_the_stock()
    {
        var (number, orderId, variantId) = await CreateOrderAsync(stock: 2, quantity: 2);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expiration = scope.ServiceProvider.GetRequiredService<PaymentExpirationService>();

        (await db.ProductVariants.Where(variant => variant.Id == variantId)
            .Select(variant => variant.StockQuantity).SingleAsync()).ShouldBe(0);

        var expired = await expiration.ExpireOverdueAsync(DateTimeOffset.UtcNow.AddHours(2));

        expired.ShouldBeGreaterThanOrEqualTo(1);

        using var verify = fixture.Factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();

        (await verifyDb.Orders.Where(order => order.Id == orderId).Select(order => order.Status).SingleAsync())
            .ShouldBe(OrderStatus.Canceled);
        (await verifyDb.Payments.Where(payment => payment.OrderId == orderId)
            .Select(payment => payment.Status).SingleAsync())
            .ShouldBe(PaymentStatus.Expired);

        // Estoque devolvido
        (await verifyDb.ProductVariants.Where(variant => variant.Id == variantId)
            .Select(variant => variant.StockQuantity).SingleAsync()).ShouldBe(2);

        _ = number;
    }

    [Fact]
    public async Task Paid_order_shows_confirmation_instead_of_pix()
    {
        var (number, _, paymentId) = await CreateOrderAsync(stock: 3, quantity: 1);

        var body = $"{{\"eventId\":\"evt-{Guid.NewGuid():N}\",\"paymentId\":\"{paymentId}\",\"orderNumber\":\"{number}\",\"amount\":24.90,\"status\":\"approved\"}}";
        var signature = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        var client = fixture.Factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/pagamentos/fake")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Signature", signature);
        (await client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync($"/pedido/{number}")).Content.ReadAsStringAsync());

        html.ShouldContain("Pagamento confirmado");
        html.ShouldNotContain("Pague com Pix");
    }

    private async Task<(string Number, Guid OrderId, string PaymentId)> CreateOrderAsync(int stock, int quantity)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        Guid variantId;

        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Pix {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create($"Sabonete Pix {suffix}", category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"PIX-{suffix}", 24.90m, stock, 100, DateTimeOffset.UtcNow);
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

        var cart = await cartService.AddItemAsync($"anon-{suffix}", variantId, quantity);
        var order = await orderService.PlaceOrderAsync(
            cart, null, $"pix.{suffix}@teste.local",
            ShippingAddress.Create("Cliente", "Rua A", "1", null, "Centro", "Tatuí", "SP", "18300000"));

        var payment = await paymentService.StartAsync(order);

        return (order.Number, order.Id, payment.ProviderPaymentId);
    }
}
