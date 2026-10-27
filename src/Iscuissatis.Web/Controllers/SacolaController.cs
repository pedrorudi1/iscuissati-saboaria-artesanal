using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

[AllowAnonymous]
public class SacolaController(CartService cartService) : Controller
{
    public const string CookieName = "iscuissati_sacola";

    [HttpGet("/sacola")]
    public async Task<IActionResult> Index(string? erro = null, CancellationToken cancellationToken = default)
    {
        var anonymousId = EnsureAnonymousId();
        var cart = await cartService.GetAsync(anonymousId, cancellationToken);

        ViewData["Title"] = "Sua sacola";
        ViewData["Error"] = erro;

        return View(cart ?? Cart.Create(anonymousId, null, DateTimeOffset.UtcNow));
    }

    [HttpPost("/sacola/adicionar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        Guid variantId,
        int quantity = 1,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var anonymousId = EnsureAnonymousId();

        try
        {
            await cartService.AddItemAsync(anonymousId, variantId, quantity, cancellationToken);
        }
        catch (Iscuissatis.Domain.Common.DomainException exception)
        {
            return RedirectToAction(nameof(Index), new { erro = exception.Message });
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/sacola/atualizar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        Guid variantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var anonymousId = EnsureAnonymousId();

        try
        {
            await cartService.ChangeQuantityAsync(anonymousId, variantId, quantity, cancellationToken);
        }
        catch (Iscuissatis.Domain.Common.DomainException exception)
        {
            return RedirectToAction(nameof(Index), new { erro = exception.Message });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/sacola/remover")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid variantId, CancellationToken cancellationToken = default)
    {
        var anonymousId = EnsureAnonymousId();
        await cartService.RemoveItemAsync(anonymousId, variantId, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    private string EnsureAnonymousId()
    {
        if (Request.Cookies.TryGetValue(CookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var anonymousId = Guid.NewGuid().ToString("N");

        Response.Cookies.Append(CookieName, anonymousId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            IsEssential = true,
        });

        return anonymousId;
    }
}
