using System.Net;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class AdminCategoriesTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Anonymous_visitor_is_redirected_to_login()
    {
        var client = fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/admin/categorias");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldBe("/entrar");
    }

    [Fact]
    public async Task Admin_creates_a_category_with_an_automatically_generated_slug()
    {
        var client = await AdminSession.SignInAsync(fixture);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/categorias/nova", "/admin/categorias/nova",
            new Dictionary<string, string>
            {
                ["Name"] = "Sabonetes em Barra",
                ["Description"] = "Barras artesanais",
                ["SortOrder"] = "1",
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldBe("/admin/categorias");

        var list = await client.GetAsync("/admin/categorias");
        var html = System.Net.WebUtility.HtmlDecode(await list.Content.ReadAsStringAsync());
        html.ShouldContain("Sabonetes em Barra");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Categories.SingleAsync(category => category.Name == "Sabonetes em Barra");
        saved.Slug.ShouldBe("sabonetes-em-barra");
    }

    [Fact]
    public async Task Second_category_with_the_same_name_receives_a_suffix()
    {
        var client = await AdminSession.SignInAsync(fixture);

        foreach (var _ in Enumerable.Range(0, 2))
        {
            await AuthHelper.PostWithAntiforgeryAsync(
                client, "/admin/categorias/nova", "/admin/categorias/nova",
                new Dictionary<string, string>
                {
                    ["Name"] = "Kits e Presentes",
                    ["Description"] = "",
                    ["SortOrder"] = "2",
                });
        }

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slugs = await db.Categories
            .Where(category => category.Name == "Kits e Presentes")
            .Select(category => category.Slug)
            .OrderBy(slug => slug)
            .ToListAsync();

        slugs.ShouldBe(["kits-e-presentes", "kits-e-presentes-2"]);
    }

    [Fact]
    public async Task Empty_name_is_rejected_with_a_message()
    {
        var client = await AdminSession.SignInAsync(fixture);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/categorias/nova", "/admin/categorias/nova",
            new Dictionary<string, string> { ["Name"] = "", ["Description"] = "", ["SortOrder"] = "0" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        html.ShouldContain("Informe o nome da categoria");
    }

    [Fact]
    public async Task Admin_deactivates_a_category()
    {
        var client = await AdminSession.SignInAsync(fixture);

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/categorias/nova", "/admin/categorias/nova",
            new Dictionary<string, string> { ["Name"] = "Novidades", ["Description"] = "", ["SortOrder"] = "3" });

        Guid id;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await db.Categories.Where(category => category.Name == "Novidades")
                .Select(category => category.Id).SingleAsync();
        }

        var page = await client.GetAsync("/admin/categorias");
        page.StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/categorias", $"/admin/categorias/{id}/ativar",
            new Dictionary<string, string> { ["active"] = "false" });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await verifyDb.Categories.SingleAsync(item => item.Id == id);
        category.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Customer_role_is_forbidden()
    {
        var email = $"cliente.cat.{Guid.NewGuid():N}@teste.local";

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Iscuissatis.Infrastructure.Identity.ApplicationUser>>();

            var customer = new Iscuissatis.Infrastructure.Identity.ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            (await users.CreateAsync(customer, AdminSession.Password)).Succeeded.ShouldBeTrue();
            (await users.AddToRoleAsync(customer, Iscuissatis.Application.Identity.Roles.Cliente)).Succeeded.ShouldBeTrue();
        }

        var client = await AuthHelper.SignInAsync(fixture.Factory, email, AdminSession.Password);

        var response = await client.GetAsync("/admin/categorias");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
