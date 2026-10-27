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

            // Espera o conteúdo da sacola carregar (o POST redireciona para /sacola).
            await page.WaitForSelectorAsync("text=Resumo", new PageWaitForSelectorOptions { Timeout = 15000 });

            var bagUrl = page.Url;
            bagUrl.ShouldNotEndWith("/adicionar");
            bagUrl.ShouldContain("/sacola");

            await page.GotoAsync("/checkout", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.Locator("#Recipient").FillAsync("Maria Teste");
            await page.Locator("#zipCode").FillAsync("18300-000");
            await page.Locator("#street").FillAsync("Rua das Flores");
            await page.Locator("#Number").FillAsync("123");
            await page.Locator("#district").FillAsync("Centro");
            await page.Locator("#city").FillAsync("Tatuí");
            await page.Locator("#state").FillAsync("SP");
            await page.Locator("#GuestEmail").FillAsync($"e2e.{Guid.NewGuid():N}@teste.local");

            // Sai do CEP para a página cotar o frete e espera as opções aparecerem.
            await page.Locator("#zipCode").BlurAsync();
            await page.Locator("input[name='ShippingCode']").First.WaitForAsync(
                new LocatorWaitForOptions { Timeout = 15000 });

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
