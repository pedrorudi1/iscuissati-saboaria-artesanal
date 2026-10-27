using Iscuissatis.Application.Catalog.Public;
using Iscuissatis.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

/// <summary>
/// Isola o controller do banco: se este teste passa, o problema do sitemap
/// está nos dados (consulta), não na paginação.
/// </summary>
public class SitemapControllerTests
{
    [Fact]
    public async Task Sitemap_includes_products_from_every_page()
    {
        var catalog = Substitute.For<ICatalogQueryService>();
        var products = Enumerable.Range(1, 120)
            .Select(index => new ProductCard(
                Guid.NewGuid(), $"Sabonete {index}", $"sabonete-{index}",
                null, null, 10m, false, "Sabonetes"))
            .ToList();

        catalog.ListCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns([new CategoryNavItem(Guid.NewGuid(), "Sabonetes", "sabonetes", 120)]);

        catalog.ListAsync(null, null, ProductSort.Relevance, 1, 48, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductCard>(products.Take(48).ToList(), 1, 48, products.Count));
        catalog.ListAsync(null, null, ProductSort.Relevance, 2, 48, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductCard>(products.Skip(48).Take(48).ToList(), 2, 48, products.Count));
        catalog.ListAsync(null, null, ProductSort.Relevance, 3, 48, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductCard>(products.Skip(96).ToList(), 3, 48, products.Count));

        var controller = new SeoController(catalog)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("loja.test");

        var result = await controller.Sitemap(CancellationToken.None) as ContentResult;

        result.ShouldNotBeNull();
        var xml = result!.Content!;

        xml.ShouldContain("/produto/sabonete-1<");
        xml.ShouldContain("/produto/sabonete-72<");
        xml.ShouldContain("/produto/sabonete-120<");
    }
}
