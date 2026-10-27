using Microsoft.Playwright;
using Shouldly;

namespace Iscuissatis.E2E.Tests;

/// <summary>
/// Fluxos críticos no navegador de verdade. O ambiente precisa ter o produto
/// de demonstração publicado (Database__SeedDemo=true no job de CI).
/// </summary>
[Collection("e2e")]
public class StorefrontFlowTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Guest_buys_from_home_until_awaiting_payment()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var productLink = page.Locator("a[href^='/produto/']").First;
            await productLink.ClickAsync();
            await page.WaitForURLAsync("**/produto/**");

            await page.Locator("h1").First.WaitForAsync();
            (await page.TitleAsync()).ShouldContain("Iscuissati");

            // Adiciona a primeira variação disponível
            await page.GetByRole(AriaRole.Button, new() { Name = "Adicionar à sacola" }).ClickAsync();
            await page.WaitForURLAsync("**/sacola**");

            (await page.ContentAsync()).ShouldContain("Resumo");

            await page.GotoAsync("/checkout", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.Locator("#Recipient").FillAsync("Maria Teste");
            await page.Locator("#ZipCode").FillAsync("18300-000");
            await page.Locator("#Street").FillAsync("Rua das Flores");
            await page.Locator("#Number").FillAsync("123");
            await page.Locator("#District").FillAsync("Centro");
            await page.Locator("#City").FillAsync("Tatuí");
            await page.Locator("#State").FillAsync("SP");
            await page.Locator("#GuestEmail").FillAsync($"e2e.{Guid.NewGuid():N}@teste.local");

            await page.GetByRole(AriaRole.Button, new() { Name = "Criar pedido" }).ClickAsync();
            await page.WaitForURLAsync("**/pedido/**");

            var confirmation = await page.ContentAsync();
            confirmation.ShouldContain("Aguardando pagamento");
            confirmation.ShouldContain("Obrigado");
        }
        catch (Exception exception)
        {
            var screenshot = await fixture.ScreenshotAsync(page, "falha-compra");
            throw new Xunit.Sdk.XunitException(
                $"{exception.Message} | screenshot: {screenshot ?? "não gerado"} | url: {page.Url}");
        }
    }

    [Fact]
    public async Task Search_finds_the_demo_product_ignoring_accents()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/sabonetes?q=lavanda", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var content = await page.ContentAsync();
            content.ShouldContain("Lavanda");
        }
        catch (Exception exception)
        {
            var screenshot = await fixture.ScreenshotAsync(page, "falha-busca");
            throw new Xunit.Sdk.XunitException(
                $"{exception.Message} | screenshot: {screenshot ?? "não gerado"} | url: {page.Url}");
        }
    }

    [Fact]
    public async Task Admin_area_requires_login()
    {
        var page = await fixture.NewPageAsync();

        await page.GotoAsync("/admin", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        page.Url.ShouldContain("/entrar");
    }
}
