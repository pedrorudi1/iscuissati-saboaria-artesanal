using System.Net;
using Iscuissatis.Application.Notifications;
using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class OrderEmailTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Confirmation_email_is_sent_once_per_order()
    {
        RecordingEmailSender.Reset();
        var (orderId, number, email) = await CreateOrderAsync();

        using var scope = fixture.Factory.Services.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<IOrderEmailNotifier>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.Include(item => item.Items).SingleAsync(item => item.Id == orderId);

        await notifier.NotifyAsync(order, OrderEmailKind.Confirmation);
        await notifier.NotifyAsync(order, OrderEmailKind.Confirmation); // reprocessamento

        RecordingEmailSender.Sent.Count(message => message.To == email).ShouldBe(1);
        RecordingEmailSender.Sent.ShouldContain(message => message.Subject.Contains(number));

        (await db.SentOrderEmails.CountAsync(item => item.OrderId == orderId)).ShouldBe(1);
    }

    [Fact]
    public async Task Cancelling_sends_the_cancellation_email()
    {
        RecordingEmailSender.Reset();
        var (orderId, number, email) = await CreateOrderAsync();

        var client = await AdminSession.SignInAsync(fixture);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, $"/admin/pedidos/{orderId}", $"/admin/pedidos/{orderId}/cancelar",
            new Dictionary<string, string>());

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        RecordingEmailSender.Sent.ShouldContain(message =>
            message.To == email && message.Subject.Contains("cancelado") && message.Subject.Contains(number));
    }

    private async Task<(Guid OrderId, string Number, string Email)> CreateOrderAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var email = $"email.{suffix}@teste.local";
        Guid variantId;

        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Email {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create($"Sabonete Email {suffix}", category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"EMA-{suffix}", 24.90m, 3, 100, DateTimeOffset.UtcNow);
            product.AddImage($"/media/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);
            product.Publish(DateTimeOffset.UtcNow);

            db.Products.Add(product);
            await db.SaveChangesAsync();
            variantId = variant.Id;
        }

        using var scope = fixture.Factory.Services.CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<CartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        var cart = await cartService.AddItemAsync($"anon-{suffix}", variantId, 1);
        var order = await orderService.PlaceOrderAsync(
            cart, null, email,
            ShippingAddress.Create("Cliente", "Rua A", "1", null, "Centro", "Tatuí", "SP", "18300000"));

        return (order.Id, order.Number, email);
    }
}
