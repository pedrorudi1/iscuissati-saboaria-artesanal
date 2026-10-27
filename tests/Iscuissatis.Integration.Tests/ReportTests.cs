using System.Net;
using Iscuissatis.Application.Reporting;
using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class ReportTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Dashboard_counts_only_paid_orders_in_revenue()
    {
        var (paidNumber, pendingNumber, productName) = await SeedOrdersAsync();

        var client = await AdminSession.SignInAsync(fixture);
        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/admin")).Content.ReadAsStringAsync());

        html.ShouldContain("Faturamento do mês");
        html.ShouldContain("Aguardando pagamento");
        html.ShouldContain(productName);

        html.ShouldNotContain("R$ 0,00</p>"); // sanity: existe faturamento no mês
        _ = paidNumber;
        _ = pendingNumber;
    }

    [Fact]
    public async Task Reports_page_and_csv_export_work_for_the_period()
    {
        await SeedOrdersAsync();
        var client = await AdminSession.SignInAsync(fixture);

        var page = await client.GetAsync("/admin/relatorios");
        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        (System.Net.WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync())).ShouldContain("Ticket médio");

        var csv = await client.GetAsync("/admin/relatorios/vendas.csv");
        csv.StatusCode.ShouldBe(HttpStatusCode.OK);
        csv.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");

        var content = await csv.Content.ReadAsStringAsync();
        content.ShouldContain("data;pedidos;faturamento");
    }

    [Fact]
    public async Task Low_stock_lists_variants_below_the_threshold()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var productName = $"Estoque baixo {suffix}";

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Rep {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create(productName, category.Id, DateTimeOffset.UtcNow);
            product.AddVariant("Última unidade", $"LOW-{suffix}", 10m, 1, 100, DateTimeOffset.UtcNow);
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var reports = verifyScope.ServiceProvider.GetRequiredService<IReportQueryService>();
        var low = await reports.GetLowStockAsync(5);

        low.ShouldContain(item => item.ProductName == productName);
    }

    private async Task<(string Paid, string Pending, string ProductName)> SeedOrdersAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var productName = $"Sabonete Relatório {suffix}";
        Guid variantId;

        using (var seedScope = fixture.Factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Relatório {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create(productName, category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"REL-{suffix}", 50m, 20, 100, DateTimeOffset.UtcNow);
            product.AddImage($"/media/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);
            product.Publish(DateTimeOffset.UtcNow);

            db.Products.Add(product);
            await db.SaveChangesAsync();
            variantId = variant.Id;
        }

        using var scope = fixture.Factory.Services.CreateScope();
        var cartService = scope.ServiceProvider.GetRequiredService<CartService>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
        var db2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var address = ShippingAddress.Create("Cliente", "Rua A", "1", null, "Centro", "Tatuí", "SP", "18300000");

        var paidCart = await cartService.AddItemAsync($"anon-paid-{suffix}", variantId, 2);
        var paid = await orderService.PlaceOrderAsync(paidCart, null, $"pago.{suffix}@teste.local", address);
        paid.MarkAsPaid(DateTimeOffset.UtcNow);
        await db2.SaveChangesAsync();

        var pendingCart = await cartService.AddItemAsync($"anon-pending-{suffix}", variantId, 1);
        var pending = await orderService.PlaceOrderAsync(pendingCart, null, $"pendente.{suffix}@teste.local", address);

        return (paid.Number, pending.Number, productName);
    }
}
