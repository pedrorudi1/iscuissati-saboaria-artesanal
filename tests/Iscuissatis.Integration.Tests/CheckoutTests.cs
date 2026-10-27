using System.Net;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class CheckoutTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Guest_checkout_creates_an_order_and_clears_the_cart()
    {
        var (client, variantId, email) = await GuestWithItemAsync(stock: 4);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/checkout", "/checkout",
            new Dictionary<string, string>
            {
                ["Recipient"] = "Maria Silva",
                ["ZipCode"] = "18300-000",
                ["Street"] = "Rua das Flores",
                ["Number"] = "123",
                ["District"] = "Centro",
                ["City"] = "Tatuí",
                ["State"] = "SP",
                ["GuestEmail"] = email,
                ["ShippingCode"] = "retirada",
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldStartWith("/pedido/");

        var confirmation = await client.GetAsync(response.Headers.Location!.OriginalString);
        var html = System.Net.WebUtility.HtmlDecode(await confirmation.Content.ReadAsStringAsync());

        confirmation.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Aguardando pagamento");
        html.ShouldContain("R$ 49,80");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await db.Orders.Include(item => item.Items)
            .SingleAsync(item => item.GuestEmail == email);
        order.Items.Count.ShouldBe(1);
        order.Items.Single().Quantity.ShouldBe(2);

        (await db.ProductVariants.Where(variant => variant.Id == variantId)
            .Select(variant => variant.StockQuantity).SingleAsync()).ShouldBe(2);

        var cartPage = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/sacola")).Content.ReadAsStringAsync());
        cartPage.ShouldContain("Sua sacola está vazia");
    }

    [Fact]
    public async Task Checkout_with_empty_cart_goes_back_to_the_bag()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/checkout");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldBe("/sacola");
    }

    [Fact]
    public async Task Invalid_zip_code_is_rejected_before_creating_the_order()
    {
        var (client, _, email) = await GuestWithItemAsync(stock: 3);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/checkout", "/checkout",
            new Dictionary<string, string>
            {
                ["Recipient"] = "Maria Silva",
                ["ZipCode"] = "123",
                ["Street"] = "Rua das Flores",
                ["Number"] = "123",
                ["District"] = "Centro",
                ["City"] = "Tatuí",
                ["State"] = "SP",
                ["GuestEmail"] = email,
                ["ShippingCode"] = "retirada",
            });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        html.ShouldContain("CEP deve ter 8 dígitos");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Orders.CountAsync(order => order.GuestEmail == email)).ShouldBe(0);
    }

    private async Task<(HttpClient Client, Guid VariantId, string Email)> GuestWithItemAsync(int stock)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        Guid variantId;

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Checkout {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create($"Sabonete Checkout {suffix}", category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"CHK-{suffix}", 24.90m, stock, 100, DateTimeOffset.UtcNow);
            product.AddImage($"/media/teste/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);
            product.Publish(DateTimeOffset.UtcNow);

            db.Products.Add(product);
            await db.SaveChangesAsync();
            variantId = variant.Id;
        }

        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/sacola", "/sacola/adicionar",
            new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "2" });

        return (client, variantId, $"checkout.{suffix}@teste.local");
    }
}
