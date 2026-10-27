using Iscuissatis.Application.Catalog.Public;
using Iscuissatis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

[AllowAnonymous]
public class CatalogoController(ICatalogQueryService catalog) : Controller
{
    [HttpGet("/sabonetes")]
    public async Task<IActionResult> Index(
        string? categoria = null,
        string? q = null,
        ProductSort ordem = ProductSort.Relevance,
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await catalog.ListAsync(categoria, q, ordem, pagina, 12, cancellationToken);
        var categories = await catalog.ListCategoriesAsync(cancellationToken);

        var model = new CatalogListViewModel
        {
            Result = result,
            Categories = categories,
            CategorySlug = categoria,
            Search = q,
            Sort = ordem,
        };

        ViewData["Title"] = model.CurrentCategoryName is null
            ? "Sabonetes artesanais"
            : model.CurrentCategoryName;

        ViewData["Description"] = model.CurrentCategoryName is null
            ? "Sabonetes artesanais feitos à mão, em pequenos lotes. Veja o catálogo completo."
            : $"Sabonetes artesanais da categoria {model.CurrentCategoryName}.";

        return View(model);
    }

    [HttpGet("/produto/{slug}")]
    public async Task<IActionResult> Produto(string slug, CancellationToken cancellationToken)
    {
        var product = await catalog.GetBySlugAsync(slug, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        ViewData["Title"] = product.Name;
        ViewData["Description"] = product.Description
            ?? $"{product.Name} — sabonete artesanal feito à mão.";

        return View(product);
    }
}
