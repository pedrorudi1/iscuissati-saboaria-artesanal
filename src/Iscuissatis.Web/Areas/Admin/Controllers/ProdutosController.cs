using Iscuissatis.Application.Catalog;
using Iscuissatis.Application.Identity;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Iscuissatis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminArea)]
public class ProdutosController(ProductService productService, CategoryService categoryService) : Controller
{
    [HttpGet("/admin/produtos")]
    public async Task<IActionResult> Index(
        string? q = null,
        ProductStatus? status = null,
        Guid? categoria = null,
        CancellationToken cancellationToken = default)
    {
        var model = new ProductListViewModel
        {
            Products = await productService.ListAsync(q, status, categoria, cancellationToken),
            Categories = await categoryService.ListAsync(cancellationToken),
            Search = q,
            Status = status,
            CategoryId = categoria,
        };

        return View(model);
    }

    [HttpGet("/admin/produtos/novo")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ProductFormViewModel
        {
            Categories = await categoryService.ListAsync(cancellationToken),
        };

        return View("Form", model);
    }

    [HttpPost("/admin/produtos/novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormModel form, CancellationToken cancellationToken)
    {
        var model = new ProductFormViewModel { Form = form };

        if (!ModelState.IsValid)
        {
            model.Categories = await categoryService.ListAsync(cancellationToken);
            return View("Form", model);
        }

        try
        {
            var product = await productService.CreateAsync(
                form.CategoryId, form.Name, form.Description, form.Ingredients,
                form.HowToUse, form.Precautions, form.NetWeightGrams, cancellationToken);

            return RedirectToEdit(product.Id, ok: $"Produto \"{product.Name}\" criado como rascunho.");
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Categories = await categoryService.ListAsync(cancellationToken);
            return View("Form", model);
        }
    }

    [HttpGet("/admin/produtos/{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        Product product;

        try
        {
            product = await productService.GetAsync(id, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        return View("Form", await BuildEditModelAsync(product, cancellationToken));
    }

    [HttpPost("/admin/produtos/{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductFormModel form, CancellationToken cancellationToken)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            var product = await productService.GetAsync(id, cancellationToken);
            var invalidModel = await BuildEditModelAsync(product, cancellationToken);
            invalidModel.Form = form;
            return View("Form", invalidModel);
        }

        try
        {
            await productService.UpdateAsync(
                id, form.CategoryId, form.Name, form.Description, form.Ingredients,
                form.HowToUse, form.Precautions, form.NetWeightGrams, cancellationToken);

            return RedirectToEdit(id, ok: "Produto atualizado.");
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            var product = await productService.GetAsync(id, cancellationToken);
            var invalidModel = await BuildEditModelAsync(product, cancellationToken);
            invalidModel.Form = form;
            return View("Form", invalidModel);
        }
    }

    [HttpPost("/admin/produtos/{id:guid}/variacoes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(Guid id, VariantFormModel newVariant, CancellationToken cancellationToken)
    {
        try
        {
            await productService.AddVariantAsync(
                id, newVariant.Name, newVariant.Sku, newVariant.Price, newVariant.StockQuantity,
                newVariant.WeightGrams, promotionalPrice: newVariant.PromotionalPrice,
                cancellationToken: cancellationToken);

            return RedirectToEdit(id, ok: "Variação adicionada.");
        }
        catch (DomainException exception)
        {
            return RedirectToEdit(id, erro: exception.Message);
        }
    }

    [HttpPost("/admin/produtos/{id:guid}/variacoes/{variantId:guid}/estoque")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(
        Guid id,
        Guid variantId,
        int delta,
        CancellationToken cancellationToken)
    {
        try
        {
            await productService.AdjustStockAsync(id, variantId, delta, cancellationToken);
            return RedirectToEdit(id, ok: delta > 0 ? "Entrada de estoque registrada." : "Saída de estoque registrada.");
        }
        catch (DomainException exception)
        {
            return RedirectToEdit(id, erro: exception.Message);
        }
    }

    [HttpPost("/admin/produtos/{id:guid}/publicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await productService.PublishAsync(id, cancellationToken);
            return RedirectToEdit(id, ok: "Produto publicado.");
        }
        catch (DomainException exception)
        {
            return RedirectToEdit(id, erro: exception.Message);
        }
    }

    [HttpPost("/admin/produtos/{id:guid}/despublicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await productService.UnpublishAsync(id, cancellationToken);
            return RedirectToEdit(id, ok: "Produto voltou para rascunho.");
        }
        catch (DomainException exception)
        {
            return RedirectToEdit(id, erro: exception.Message);
        }
    }

    private RedirectToActionResult RedirectToEdit(Guid id, string? ok = null, string? erro = null)
        => RedirectToAction(nameof(Edit), new { id, ok, erro });

    private async Task<ProductFormViewModel> BuildEditModelAsync(Product product, CancellationToken cancellationToken)
        => new()
        {
            Form = new ProductFormModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Description = product.Description,
                Ingredients = product.Ingredients,
                HowToUse = product.HowToUse,
                Precautions = product.Precautions,
                NetWeightGrams = product.NetWeightGrams,
            },
            Categories = await categoryService.ListAsync(cancellationToken),
            Variants = product.Variants.ToList(),
            ImageCount = product.Images.Count,
            IsPublished = product.IsPublished,
        };
}
