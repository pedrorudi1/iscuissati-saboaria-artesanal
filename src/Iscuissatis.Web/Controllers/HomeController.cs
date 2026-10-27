using Iscuissatis.Application.Catalog.Public;
using Iscuissatis.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

public class HomeController(ICatalogQueryService catalog) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var featured = await catalog.ListAsync(sort: ProductSort.Newest, pageSize: 8, cancellationToken: cancellationToken);
        var categories = await catalog.ListCategoriesAsync(cancellationToken);

        return View(new HomeViewModel
        {
            Categories = categories,
            Featured = featured.Items,
            PublishedCount = featured.TotalItems,
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
