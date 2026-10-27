using System.Net;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class SacolaTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Guest_adds_an_item_and_sees_it_in_the_cart()
    {
        var (client, variantId, productName) = await NewGuestWithProductAsync(stock: 5);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/sacola", "/sacola/adicionar",
            new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "2" });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/sacola")).Content.ReadAsStringAsync());

        html.ShouldContain(productName);
        html.ShouldContain("R$ 24,90");
    }

    [Fact]
    public async Task Adding_above_stock_shows_an_error_and_keeps_the_cart()
    {
        var (client, variantId, _) = await NewGuestWithProductAsync(stock: 1);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/sacola", "/sacola/adicionar",
            new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "5" });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync(response.Headers.Location!.OriginalString)).Content.ReadAsStringAsync());

        html.ShouldContain("Estoque insuficiente");
        html.ShouldContain("Sua sacola está vazia");
    }

    [Fact]
    public async Task Quantity_zero_removes_the_item()
    {
        var (client, variantId, productName) = await NewGuestWithProductAsync(stock: 5);

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/sacola", "/sacola/adicionar",
            new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "1" });

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/sacola", "/sacola/atualizar",
            new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "0" });

        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/sacola")).Content.ReadAsStringAsync());

        html.ShouldContain("Sua sacola está vazia");
        html.ShouldNotContain(productName);
    }

    [Fact]
    public async Task Same_variant_twice_stays_in_a_single_line()
    {
        var (client, variantId, _) = await NewGuestWithProductAsync(stock: 5);

        foreach (var _ in Enumerable.Range(0, 2))
        {
            await AuthHelper.PostWithAntiforgeryAsync(
                client, "/sacola", "/sacola/adicionar",
                new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "1" });
        }

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var items = await db.CartItems.Where(item => item.ProductVariantId == variantId).ToListAsync();

        items.Count.ShouldBe(1);
        items[0].Quantity.ShouldBe(2);
        (items[0].UnitPrice * items[0].Quantity).ShouldBe(49.80m);
    }

    private async Task<(HttpClient Client, Guid VariantId, string ProductName)> NewGuestWithProductAsync(int stock)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var productName = $"Sabonete Sacola {suffix}";

        Guid variantId;

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Sacola {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create(productName, category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"SAC-{suffix}", 24.90m, stock, 100, DateTimeOffset.UtcNow);
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

        return (client, variantId, productName);
    }
}
