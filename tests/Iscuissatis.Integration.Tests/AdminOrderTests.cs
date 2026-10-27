using System.Net;
using Iscuissatis.Application.Identity;
using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class AdminOrderTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Admin_lists_orders_and_finds_them_by_number()
    {
        var (orderId, number) = await CreateOrderAsync(stock: 3);

        var client = await AdminSession.SignInAsync(fixture);

        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync($"/admin/pedidos?q={number}")).Content.ReadAsStringAsync());

        html.ShouldContain(number);

        var details = await client.GetAsync($"/admin/pedidos/{orderId}");
        details.StatusCode.ShouldBe(HttpStatusCode.OK);
        (System.Net.WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync()))
            .ShouldContain("Aguardando pagamento");
    }

    [Fact]
    public async Task Cancelling_from_the_panel_returns_the_stock()
    {
        var (orderId, _) = await CreateOrderAsync(stock: 2);
        var client = await AdminSession.SignInAsync(fixture);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, $"/admin/pedidos/{orderId}", $"/admin/pedidos/{orderId}/cancelar",
            new Dictionary<string, string>());

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await db.Orders.FindAsync(orderId);
        order!.Status.ShouldBe(OrderStatus.Canceled);

        var reservation = await db.StockReservations.SingleAsync(item => item.OrderId == orderId);
        reservation.ReleasedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Customer_cannot_reach_the_order_panel()
    {
        var email = $"cliente.pedidos.{Guid.NewGuid():N}@teste.local";

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            (await users.CreateAsync(user, AdminSession.Password)).Succeeded.ShouldBeTrue();
            (await users.AddToRoleAsync(user, Roles.Cliente)).Succeeded.ShouldBeTrue();
        }

        var client = await AuthHelper.SignInAsync(fixture.Factory, email, AdminSession.Password);
        var response = await client.GetAsync("/admin/pedidos");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<(Guid OrderId, string Number)> CreateOrderAsync(int stock)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        Guid variantId;

        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var category = Category.Create($"Pedido admin {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create($"Sabonete Admin {suffix}", category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"ADM-{suffix}", 24.90m, stock, 100, DateTimeOffset.UtcNow);
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
            cart,
            null,
            $"admin.pedido.{suffix}@teste.local",
            ShippingAddress.Create("Cliente Teste", "Rua A", "1", null, "Centro", "Tatuí", "SP", "18300000"));

        return (order.Id, order.Number);
    }
}
