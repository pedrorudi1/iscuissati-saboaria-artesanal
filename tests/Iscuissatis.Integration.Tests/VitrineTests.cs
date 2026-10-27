using System.Net;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class VitrineTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Draft_products_do_not_appear_and_their_page_is_404()
    {
        var (published, draft) = await SeedPairAsync();

        var client = fixture.Factory.CreateClient();

        var listing = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/sabonetes")).Content.ReadAsStringAsync());
        listing.ShouldContain(published.Name);
        listing.ShouldNotContain(draft.Name);

        var draftPage = await client.GetAsync($"/produto/{draft.Slug}");
        draftPage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_ignores_accents_and_case()
    {
        var (product, _) = await SeedPairAsync(name: "Sabonete de Calêndula");

        var client = fixture.Factory.CreateClient();
        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/sabonetes?q=calendula")).Content.ReadAsStringAsync());

        html.ShouldContain(product.Name);
    }

    [Fact]
    public async Task Sorting_by_price_puts_the_cheapest_first()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var expensive = await SeedProductAsync($"Caro {suffix}", published: true, price: 90m);
        var cheap = await SeedProductAsync($"Barato {suffix}", published: true, price: 12m);

        var client = fixture.Factory.CreateClient();
        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync($"/sabonetes?q={suffix}&ordem=1")).Content.ReadAsStringAsync());

        var cheapIndex = html.IndexOf(cheap.Name, StringComparison.Ordinal);
        var expensiveIndex = html.IndexOf(expensive.Name, StringComparison.Ordinal);

        cheapIndex.ShouldBeGreaterThanOrEqualTo(0);
        expensiveIndex.ShouldBeGreaterThanOrEqualTo(0);
        cheapIndex.ShouldBeLessThan(expensiveIndex);
    }

    [Fact]
    public async Task Product_page_renders_json_ld_and_price()
    {
        var (product, _) = await SeedPairAsync(name: "Sabonete de Lavanda");

        var client = fixture.Factory.CreateClient();
        var response = await client.GetAsync($"/produto/{product.Slug}");
        var html = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("application/ld+json");
        html.ShouldContain("schema.org");
        html.ShouldContain(product.Name);
        html.ShouldContain("24,90");
    }

    [Fact]
    public async Task Product_from_inactive_category_is_not_visible()
    {
        var name = $"Oculto {Guid.NewGuid():N}"[..16];
        var product = await SeedProductAsync(name, published: true, price: 30m, categoryActive: false);

        var client = fixture.Factory.CreateClient();

        var page = await client.GetAsync($"/produto/{product.Slug}");
        page.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var listing = await (await client.GetAsync("/sabonetes")).Content.ReadAsStringAsync();
        listing.ShouldNotContain(name);
    }

    private async Task<(Product Published, Product Draft)> SeedPairAsync(string name = "Sabonete Artesanal")
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var published = await SeedProductAsync($"{name} {suffix}", published: true, price: 24.90m);
        var draft = await SeedProductAsync($"Rascunho {suffix}", published: false, price: 19.90m);

        return (published, draft);
    }

    private async Task<Product> SeedProductAsync(
        string name,
        bool published,
        decimal price,
        bool categoryActive = true)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = Category.Create($"Vitrine {Guid.NewGuid():N}"[..18], DateTimeOffset.UtcNow);
        if (!categoryActive)
        {
            category.Deactivate(DateTimeOffset.UtcNow);
        }

        db.Categories.Add(category);

        var product = Product.Create(name, category.Id, DateTimeOffset.UtcNow, description: "Barra artesanal");
        product.AddVariant("100 g", $"SKU-{Guid.NewGuid():N}"[..16], price, 5, 100, DateTimeOffset.UtcNow);
        product.AddImage($"/media/teste/{Guid.NewGuid():N}.webp", "Foto do sabonete", DateTimeOffset.UtcNow, 800, 600);

        if (published)
        {
            product.Publish(DateTimeOffset.UtcNow);
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return product;
    }
}
