using System.Net;
using Iscuissatis.Application.Identity;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class LgpdTests(IntegrationTestFixture fixture)
{
    private const string Password = "SenhaDoCliente123";

    [Fact]
    public async Task Sitemap_lists_only_published_products()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var publishedSlug = await SeedProductAsync($"Publicado {suffix}", publish: true);
        var draftSlug = await SeedProductAsync($"Rascunho {suffix}", publish: false);

        var client = fixture.Factory.CreateClient();
        var xml = await (await client.GetAsync("/sitemap.xml")).Content.ReadAsStringAsync();

        xml.ShouldContain(publishedSlug);
        xml.ShouldNotContain(draftSlug);
        xml.ShouldContain("/privacidade");
    }

    [Fact]
    public async Task Robots_blocks_private_areas_and_points_to_the_sitemap()
    {
        var client = fixture.Factory.CreateClient();
        var body = await (await client.GetAsync("/robots.txt")).Content.ReadAsStringAsync();

        body.ShouldContain("Disallow: /admin");
        body.ShouldContain("Disallow: /checkout");
        body.ShouldContain("Disallow: /minha-conta");
        body.ShouldContain("Sitemap:");
    }

    [Fact]
    public async Task Institutional_pages_render_with_a_provisional_notice()
    {
        var client = fixture.Factory.CreateClient();

        foreach (var path in new[] { "/sobre", "/contato", "/faq", "/trocas-e-devolucoes", "/privacidade", "/termos" })
        {
            var response = await client.GetAsync(path);
            var html = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            html.ShouldContain("Texto provisório");
        }
    }

    [Fact]
    public async Task Cookie_banner_and_analytics_gate_are_present_on_the_storefront()
    {
        var client = fixture.Factory.CreateClient();
        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        html.ShouldContain("cookie-banner");
        html.ShouldContain("data-consent=\"granted\"");
        html.ShouldContain("name=\"ga4-id\"");
    }

    [Fact]
    public async Task Customer_can_export_their_data_as_json()
    {
        var email = await CreateCustomerAsync();
        var client = await AuthHelper.SignInAsync(fixture.Factory, email, Password);

        var response = await client.GetAsync("/minha-conta/dados/exportar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain(email);
        json.ShouldContain("pedidos");
    }

    [Fact]
    public async Task Customer_can_request_deletion_and_gets_anonymized()
    {
        var email = await CreateCustomerAsync();
        var client = await AuthHelper.SignInAsync(fixture.Factory, email, Password);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/minha-conta/dados", "/minha-conta/dados/excluir",
            new Dictionary<string, string>());

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        (await users.FindByEmailAsync(email)).ShouldBeNull();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var anonymized = await db.Users.SingleAsync(user => user.FullName == "Conta removida" && user.Email!.Contains("anonimizado.local"));
        anonymized.Email!.ShouldNotContain("teste.local");
    }

    private async Task<string> CreateCustomerAsync()
    {
        var email = $"lgpd.{Guid.NewGuid():N}@teste.local";

        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Cliente LGPD",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        (await users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        (await users.AddToRoleAsync(user, Roles.Cliente)).Succeeded.ShouldBeTrue();

        return email;
    }

    private async Task<string> SeedProductAsync(string name, bool publish)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = Category.Create($"SEO {suffix}", DateTimeOffset.UtcNow);
        db.Categories.Add(category);

        var product = Product.Create(name, category.Id, DateTimeOffset.UtcNow);
        product.AddVariant("100 g", $"SEO-{suffix}", 10m, 5, 100, DateTimeOffset.UtcNow);
        product.AddImage($"/media/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);

        if (publish)
        {
            product.Publish(DateTimeOffset.UtcNow);
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return $"/produto/{product.Slug}";
    }
}
