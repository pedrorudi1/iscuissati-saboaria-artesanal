using System.Net;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class LayoutTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Home_renders_the_storefront_layout_in_portuguese()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("lang=\"pt-BR\"");
        html.ShouldContain("Sabonetes artesanais para o seu dia a dia");
        html.ShouldContain("/css/site.css");
        html.ShouldContain("Ir para o conteúdo"); // link de pular navegação (a11y)
    }

    [Fact]
    public async Task Login_page_renders_the_form_with_antiforgery_token()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/entrar");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Entrar no painel");
        html.ShouldContain("__RequestVerificationToken");
        html.ShouldContain("name=\"robots\""); // página de login não deve ser indexada
    }

    [Fact]
    public async Task Admin_dashboard_uses_the_admin_layout()
    {
        var client = await AuthHelper.SignInAsync(
            fixture.Factory, IntegrationTestFixture.AdminEmail, IntegrationTestFixture.AdminPassword);

        var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Área administrativa");
        html.ShouldContain("Sair");
        html.ShouldContain("Ver a loja");
    }
}
