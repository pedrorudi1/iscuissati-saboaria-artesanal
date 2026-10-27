using System.Net;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class AdminProductsTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Anonymous_visitor_is_redirected_to_login()
    {
        var client = fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/admin/produtos");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldBe("/entrar");
    }

    [Fact]
    public async Task Admin_creates_a_product_as_draft()
    {
        var client = await AdminSession.SignInAsync(fixture);
        var categoryId = await CreateCategoryAsync("Sabonetes em Barra");

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/produtos/novo", "/admin/produtos/novo",
            new Dictionary<string, string>
            {
                ["Form.Name"] = "Sabonete de Lavanda",
                ["Form.CategoryId"] = categoryId.ToString(),
                ["Form.Description"] = "Barra de 100 g",
                ["Form.NetWeightGrams"] = "100",
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products.SingleAsync(item => item.Name == "Sabonete de Lavanda");

        product.Slug.ShouldBe("sabonete-de-lavanda");
        product.Status.ShouldBe(ProductStatus.Draft);
    }

    [Fact]
    public async Task Search_filters_the_product_list_by_name()
    {
        var client = await AdminSession.SignInAsync(fixture);
        var categoryId = await CreateCategoryAsync("Filtros");

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/produtos/novo", "/admin/produtos/novo",
            new Dictionary<string, string>
            {
                ["Form.Name"] = "Alecrim e Hortelã",
                ["Form.CategoryId"] = categoryId.ToString(),
            });

        var found = await client.GetAsync("/admin/produtos?q=alecrim");
        (await found.Content.ReadAsStringAsync()).ShouldContain("Alecrim e Hortelã");

        var missing = await client.GetAsync("/admin/produtos?q=lavanda");
        (await missing.Content.ReadAsStringAsync()).ShouldNotContain("Alecrim e Hortelã");
    }

    [Fact]
    public async Task Publishing_without_image_is_blocked_with_an_explanation()
    {
        var client = await AdminSession.SignInAsync(fixture);
        var categoryId = await CreateCategoryAsync("Publicação");
        var productId = await CreateProductAsync(client, categoryId, "Calêndula");

        await AuthHelper.PostWithAntiforgeryAsync(
            client, $"/admin/produtos/{productId}/editar", $"/admin/produtos/{productId}/variacoes",
            new Dictionary<string, string>
            {
                ["NewVariant.Name"] = "Calêndula 100 g",
                ["NewVariant.Sku"] = $"CAL-{Guid.NewGuid():N}"[..12],
                ["NewVariant.Price"] = "27.90",
                ["NewVariant.StockQuantity"] = "8",
                ["NewVariant.WeightGrams"] = "100",
            });

        var publish = await AuthHelper.PostWithAntiforgeryAsync(
            client, $"/admin/produtos/{productId}/editar", $"/admin/produtos/{productId}/publicar",
            new Dictionary<string, string>());

        publish.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var page = await client.GetAsync($"/admin/produtos/{productId}/editar");
        var html = System.Net.WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
        html.ShouldContain("imagem");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Products.FindAsync(productId))!.Status.ShouldBe(ProductStatus.Draft);
    }

    [Fact]
    public async Task Sku_already_used_by_another_product_is_rejected()
    {
        var client = await AdminSession.SignInAsync(fixture);
        var categoryId = await CreateCategoryAsync("SKU");
        var first = await CreateProductAsync(client, categoryId, "Aveia e Mel");
        var second = await CreateProductAsync(client, categoryId, "Argila Verde");

        var sku = $"AVE-{Guid.NewGuid():N}"[..12];
        var firstAdd = await AuthHelper.PostWithAntiforgeryAsync(
            client, $"/admin/produtos/{first}/editar", $"/admin/produtos/{first}/variacoes",
            new Dictionary<string, string>
            {
                ["NewVariant.Name"] = "Aveia 100 g",
                ["NewVariant.Sku"] = sku,
                ["NewVariant.Price"] = "22.00",
                ["NewVariant.StockQuantity"] = "3",
                ["NewVariant.WeightGrams"] = "100",
            });
        firstAdd.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await AuthHelper.PostWithAntiforgeryAsync(
            client, $"/admin/produtos/{second}/editar", $"/admin/produtos/{second}/variacoes",
            new Dictionary<string, string>
            {
                ["NewVariant.Name"] = "Argila 100 g",
                ["NewVariant.Sku"] = sku.ToLowerInvariant(),
                ["NewVariant.Price"] = "22.00",
                ["NewVariant.StockQuantity"] = "3",
                ["NewVariant.WeightGrams"] = "100",
            });

        var page = await client.GetAsync($"/admin/produtos/{second}/editar");
        var html = System.Net.WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
        html.ShouldContain("já está em uso");
    }

    private async Task<Guid> CreateCategoryAsync(string name)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = Category.Create(name, DateTimeOffset.UtcNow);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category.Id;
    }

    private async Task<Guid> CreateProductAsync(HttpClient client, Guid categoryId, string name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/produtos/novo", "/admin/produtos/novo",
            new Dictionary<string, string>
            {
                ["Form.Name"] = $"{name} {suffix}",
                ["Form.CategoryId"] = categoryId.ToString(),
            });

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Products
            .Where(product => product.Name == $"{name} {suffix}")
            .Select(product => product.Id)
            .SingleAsync();
    }
}
