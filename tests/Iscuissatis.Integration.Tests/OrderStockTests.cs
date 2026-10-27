using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class OrderStockTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Placing_an_order_reserves_stock_and_starts_as_pending_payment()
    {
        var variantId = await SeedProductAsync(stock: 5);
        var scope = fixture.Factory.Services.CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<CartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        var cart = await cartService.AddItemAsync($"anon-{Guid.NewGuid():N}", variantId, 2);

        var order = await orderService.PlaceOrderAsync(cart, null, "cliente@teste.com", Address());

        order.Status.ShouldBe(OrderStatus.PendingPayment);
        order.Subtotal.ShouldBe(49.80m);

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await db.ProductVariants.Where(variant => variant.Id == variantId)
            .Select(variant => variant.StockQuantity).SingleAsync()).ShouldBe(3);

        var reservation = await db.StockReservations.SingleAsync(item => item.OrderId == order.Id);
        reservation.Quantity.ShouldBe(2);
        reservation.IsReleased.ShouldBeFalse();
    }

    [Fact]
    public async Task A_stale_cart_cannot_buy_the_last_unit_twice()
    {
        var variantId = await SeedProductAsync(stock: 1);

        var firstScope = fixture.Factory.Services.CreateScope();
        var firstCartService = firstScope.ServiceProvider.GetRequiredService<CartService>();
        var firstOrderService = firstScope.ServiceProvider.GetRequiredService<OrderService>();
        var firstCart = await firstCartService.AddItemAsync($"anon-{Guid.NewGuid():N}", variantId, 1);

        var secondScope = fixture.Factory.Services.CreateScope();
        var secondCartService = secondScope.ServiceProvider.GetRequiredService<CartService>();
        var secondOrderService = secondScope.ServiceProvider.GetRequiredService<OrderService>();
        var secondCart = await secondCartService.AddItemAsync($"anon-{Guid.NewGuid():N}", variantId, 1);

        await firstOrderService.PlaceOrderAsync(firstCart, null, "primeiro@teste.com", Address());

        var error = await Should.ThrowAsync<DomainException>(() =>
            secondOrderService.PlaceOrderAsync(secondCart, null, "segundo@teste.com", Address()));

        error.Message.ShouldContain("Estoque insuficiente");

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.ProductVariants.Where(variant => variant.Id == variantId)
            .Select(variant => variant.StockQuantity).SingleAsync()).ShouldBe(0);
        (await db.Orders.CountAsync(order => order.GuestEmail == "segundo@teste.com")).ShouldBe(0);
    }

    [Fact]
    public async Task Concurrent_orders_for_the_last_unit_only_one_wins()
    {
        var variantId = await SeedProductAsync(stock: 1);

        var carts = new List<Cart>();

        for (var index = 0; index < 2; index++)
        {
            var scope = fixture.Factory.Services.CreateScope();
            var cartService = scope.ServiceProvider.GetRequiredService<CartService>();
            carts.Add(await cartService.AddItemAsync($"anon-{Guid.NewGuid():N}", variantId, 1));
        }

        var attempts = carts.Select(cart => Task.Run(async () =>
        {
            using var scope = fixture.Factory.Services.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            try
            {
                await orderService.PlaceOrderAsync(cart, null, $"{cart.Id}@teste.com", Address());
                return true;
            }
            catch (DomainException)
            {
                return false;
            }
        })).ToArray();

        var results = await Task.WhenAll(attempts);

        results.Count(success => success).ShouldBe(1);

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.ProductVariants.Where(variant => variant.Id == variantId)
            .Select(variant => variant.StockQuantity).SingleAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Cancelling_an_order_releases_the_stock()
    {
        var variantId = await SeedProductAsync(stock: 2);
        using var scope = fixture.Factory.Services.CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<CartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        var cart = await cartService.AddItemAsync($"anon-{Guid.NewGuid():N}", variantId, 2);
        var order = await orderService.PlaceOrderAsync(cart, null, "cancelar@teste.com", Address());

        await orderService.CancelAsync(order.Id);

        order.Status.ShouldBe(OrderStatus.Canceled);

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.ProductVariants.Where(variant => variant.Id == variantId)
            .Select(variant => variant.StockQuantity).SingleAsync()).ShouldBe(2);
        (await db.StockReservations.Where(item => item.OrderId == order.Id)
            .Select(item => item.ReleasedAtUtc != null).SingleAsync()).ShouldBeTrue();
    }

    private static ShippingAddress Address()
        => ShippingAddress.Create("Maria Silva", "Rua das Flores", "123", null, "Centro", "Tatuí", "SP", "18300000");

    private async Task<Guid> SeedProductAsync(int stock)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = Category.Create($"Pedidos {suffix}", DateTimeOffset.UtcNow);
        db.Categories.Add(category);

        var product = Product.Create($"Sabonete Pedido {suffix}", category.Id, DateTimeOffset.UtcNow);
        var variant = product.AddVariant("100 g", $"PED-{suffix}", 24.90m, stock, 100, DateTimeOffset.UtcNow);
        product.AddImage($"/media/teste/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);
        product.Publish(DateTimeOffset.UtcNow);

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return variant.Id;
    }
}
