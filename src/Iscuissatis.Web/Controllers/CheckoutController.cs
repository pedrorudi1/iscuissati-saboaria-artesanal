using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

[AllowAnonymous]
public class CheckoutController(
    CartService cartService,
    OrderService orderService,
    IZipCodeLookup zipCodeLookup,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("/checkout")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cart = await LoadCartAsync(cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Sacola");
        }

        ViewData["Title"] = "Finalizar compra";
        ViewData["Cart"] = cart;

        return View(new CheckoutFormModel
        {
            Recipient = User.Identity?.IsAuthenticated == true ? User.Identity.Name ?? string.Empty : string.Empty,
            GuestEmail = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null,
        });
    }

    [HttpPost("/checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutFormModel form, CancellationToken cancellationToken)
    {
        var cart = await LoadCartAsync(cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Sacola");
        }

        ViewData["Title"] = "Finalizar compra";
        ViewData["Cart"] = cart;

        if (User.Identity?.IsAuthenticated != true && string.IsNullOrWhiteSpace(form.GuestEmail))
        {
            ModelState.AddModelError(nameof(form.GuestEmail), "Informe um e-mail para contato.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            var address = ShippingAddress.Create(
                form.Recipient, form.Street, form.Number, form.Complement,
                form.District ?? string.Empty, form.City, form.State, form.ZipCode);

            Guid? customerId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                customerId = (await userManager.GetUserAsync(User))?.Id;
            }

            var order = await orderService.PlaceOrderAsync(
                cart,
                customerId,
                customerId is null ? form.GuestEmail : null,
                address,
                cancellationToken);

            await cartService.ClearCartAsync(cart, cancellationToken);

            return Redirect($"/pedido/{order.Number}");
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(form);
        }
    }

    [HttpGet("/checkout/cep/{zipCode}")]
    public async Task<IActionResult> LookupZipCode(string zipCode, CancellationToken cancellationToken)
    {
        var address = await zipCodeLookup.FindAsync(zipCode, cancellationToken);

        if (address is null)
        {
            return NotFound(new { message = "CEP não encontrado. Preencha manualmente." });
        }

        return Ok(address);
    }

    [HttpGet("/pedido/{number}")]
    public async Task<IActionResult> Confirmation(string number, CancellationToken cancellationToken)
    {
        var order = await orderService.GetByNumberAsync(number, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Pedido {order.Number}";
        return View("Confirmacao", order);
    }

    private Task<Cart?> LoadCartAsync(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(SacolaController.CookieName, out var anonymousId)
            || string.IsNullOrWhiteSpace(anonymousId))
        {
            return Task.FromResult<Cart?>(null);
        }

        return cartService.GetAsync(anonymousId, cancellationToken);
    }
}
