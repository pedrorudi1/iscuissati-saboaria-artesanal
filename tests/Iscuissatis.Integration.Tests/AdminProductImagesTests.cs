using System.Net;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class AdminProductImagesTests(IntegrationTestFixture fixture)
{
    // PNG 1x1 válido: serve para provar que a validação é por conteúdo.
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    [Fact]
    public async Task Upload_converts_the_file_to_webp_and_persists_the_image()
    {
        var client = await AdminSession.SignInAsync(fixture);
        var productId = await CreateProductAsync(client, "Lavanda com foto");

        var response = await AuthHelper.PostMultipartAsync(
            client,
            $"/admin/produtos/{productId}/editar",
            $"/admin/produtos/{productId}/imagens",
            "file",
            "foto-original.png",
            Convert.FromBase64String(OnePixelPngBase64),
            "image/png",
            new Dictionary<string, string> { ["altText"] = "Sabonete de lavanda sobre mesa" });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var image = await db.ProductImages.SingleAsync(item => item.ProductId == productId);

        image.Path.ShouldEndWith(".webp");
        image.Width.ShouldBeGreaterThan(0);
        image.Height.ShouldBeGreaterThan(0);
        image.IsPrimary.ShouldBeTrue();
        image.AltText.ShouldBe("Sabonete de lavanda sobre mesa");

        var relative = image.Path.Replace("/media/", string.Empty, StringComparison.Ordinal);
        var fullPath = Path.Combine(IntegrationTestFixture.StorageRoot, relative);
        File.Exists(fullPath).ShouldBeTrue($"arquivo esperado em {fullPath}");
    }

    [Fact]
    public async Task Upload_rejects_a_pdf_renamed_as_jpg()
    {
        var client = await AdminSession.SignInAsync(fixture);
        var productId = await CreateProductAsync(client, "Arquivo falso");

        var fakePdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 conteudo que nao e imagem");

        var response = await AuthHelper.PostMultipartAsync(
            client,
            $"/admin/produtos/{productId}/editar",
            $"/admin/produtos/{productId}/imagens",
            "file",
            "nota-fiscal.jpg",
            fakePdf,
            "image/jpeg",
            new Dictionary<string, string> { ["altText"] = "Tentativa de burlar a validação" });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var page = await client.GetAsync(response.Headers.Location!.OriginalString);
        var html = System.Net.WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
        html.ShouldContain("não é uma imagem válida");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.ProductImages.CountAsync(item => item.ProductId == productId)).ShouldBe(0);
    }

    [Fact]
    public async Task Removing_an_image_deletes_the_file_from_storage()
    {
        var client = await AdminSession.SignInAsync(fixture);
        var productId = await CreateProductAsync(client, "Remoção de foto");

        await AuthHelper.PostMultipartAsync(
            client,
            $"/admin/produtos/{productId}/editar",
            $"/admin/produtos/{productId}/imagens",
            "file",
            "foto.png",
            Convert.FromBase64String(OnePixelPngBase64),
            "image/png",
            new Dictionary<string, string> { ["altText"] = "Foto que será removida" });

        Guid imageId;
        string path;

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var image = await db.ProductImages.SingleAsync(item => item.ProductId == productId);
            imageId = image.Id;
            path = image.Path;
        }

        var fullPath = Path.Combine(
            IntegrationTestFixture.StorageRoot,
            path.Replace("/media/", string.Empty, StringComparison.Ordinal));

        File.Exists(fullPath).ShouldBeTrue();

        await AuthHelper.PostWithAntiforgeryAsync(
            client,
            $"/admin/produtos/{productId}/editar",
            $"/admin/produtos/{productId}/imagens/{imageId}/remover",
            new Dictionary<string, string>());

        File.Exists(fullPath).ShouldBeFalse();

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.ProductImages.CountAsync(item => item.ProductId == productId)).ShouldBe(0);
    }

    private async Task<Guid> CreateProductAsync(HttpClient client, string name)
    {
        Guid categoryId;

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Imagens {Guid.NewGuid():N}"[..20], DateTimeOffset.UtcNow);
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            categoryId = category.Id;
        }

        var unique = $"{name} {Guid.NewGuid():N}"[..(name.Length + 7)];

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/produtos/novo", "/admin/produtos/novo",
            new Dictionary<string, string>
            {
                ["Form.Name"] = unique,
                ["Form.CategoryId"] = categoryId.ToString(),
            });

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await verifyDb.Products.Where(product => product.Name == unique)
            .Select(product => product.Id).SingleAsync();
    }
}
