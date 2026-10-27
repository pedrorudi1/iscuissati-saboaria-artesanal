using System.Text;
using System.Xml;
using Iscuissatis.Application.Catalog.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

[AllowAnonymous]
public class SeoController(ICatalogQueryService catalog) : Controller
{
    private static readonly string[] PublicPaths =
    [
        "/", "/sabonetes", "/sobre", "/contato", "/faq", "/trocas-e-devolucoes", "/privacidade", "/termos",
    ];

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var products = await catalog.ListAsync(pageSize: 48, cancellationToken: cancellationToken);
        var categories = await catalog.ListCategoriesAsync(cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using var stream = new MemoryStream();
        await using (var writer = XmlWriter.Create(stream, settings))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var path in PublicPaths)
            {
                await WriteUrlAsync(writer, baseUrl + path);
            }

            foreach (var category in categories)
            {
                await WriteUrlAsync(writer, $"{baseUrl}/sabonetes?categoria={category.Slug}");
            }

            foreach (var product in products.Items)
            {
                await WriteUrlAsync(writer, $"{baseUrl}/produto/{product.Slug}");
            }

            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
        }

        return Content(Encoding.UTF8.GetString(stream.ToArray()), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var body = $"""
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /checkout
            Disallow: /minha-conta
            Disallow: /sacola
            Disallow: /entrar
            Disallow: /criar-conta

            Sitemap: {baseUrl}/sitemap.xml
            """;

        return Content(body, "text/plain", Encoding.UTF8);
    }

    private static async Task WriteUrlAsync(XmlWriter writer, string location)
    {
        await writer.WriteStartElementAsync(null, "url", null);
        await writer.WriteElementStringAsync(null, "loc", null, location);
        await writer.WriteEndElementAsync();
    }
}
