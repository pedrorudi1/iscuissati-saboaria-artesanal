using Iscuissatis.Application.Catalog;
using Iscuissatis.Application.Identity;
using Iscuissatis.Domain.Common;
using Iscuissatis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminArea)]
public class CategoriasController(CategoryService categoryService) : Controller
{
    [HttpGet("/admin/categorias")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await categoryService.ListAsync(cancellationToken);
        return View(categories);
    }

    [HttpGet("/admin/categorias/nova")]
    public IActionResult Create() => View("Form", new CategoryFormModel());

    [HttpPost("/admin/categorias/nova")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await categoryService.CreateAsync(model.Name, model.Description, model.SortOrder, cancellationToken);
            TempData["CategoryMessage"] = $"Categoria \"{model.Name.Trim()}\" criada.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", model);
        }
    }

    [HttpGet("/admin/categorias/{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var categories = await categoryService.ListAsync(cancellationToken);
        var category = categories.FirstOrDefault(item => item.Id == id);

        if (category is null)
        {
            return NotFound();
        }

        return View("Form", new CategoryFormModel
        {
            Name = category.Name,
            Description = category.Description,
            SortOrder = category.SortOrder,
            Id = category.Id,
        });
    }

    [HttpPost("/admin/categorias/{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryFormModel model, CancellationToken cancellationToken)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await categoryService.UpdateAsync(id, model.Name, model.Description, model.SortOrder, cancellationToken);
            TempData["CategoryMessage"] = $"Categoria \"{model.Name.Trim()}\" atualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", model);
        }
    }

    [HttpPost("/admin/categorias/{id:guid}/ativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id, bool active, CancellationToken cancellationToken)
    {
        try
        {
            await categoryService.SetActiveAsync(id, active, cancellationToken);
            TempData["CategoryMessage"] = active ? "Categoria ativada." : "Categoria desativada.";
        }
        catch (DomainException exception)
        {
            TempData["CategoryMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
