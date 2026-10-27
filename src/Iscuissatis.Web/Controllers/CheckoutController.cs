using Iscuissatis.Application.Sales;
using Iscuissatis.Application.Notifications;
using Iscuissatis.Application.Shipping;
using Iscuissatis.Application.Catalog;
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
    UserManager<ApplicationUser> userManager,
    AddressService addressService,
    IShippingProvider shippingProvider,
    IProductRepository products,
    IOrderEmailNotifier orderEmails) : Controller
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

        var form = new CheckoutFormModel
        {
            Recipient = User.Identity?.IsAuthenticated == true ? User.Identity.Name ?? string.Empty : string.Empty,
            GuestEmail = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null,
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var identityUser = await userManager.GetUserAsync(User);

            if (Guid.TryParse(identityUser?.Id, out var customerId))
            {
                var saved = (await addressService.ListAsync(customerId, cancellationToken))
                    .FirstOrDefault(address => address.IsDefault);

                if (saved is not null)
                {
                    form.Recipient = saved.Recipient;
                    form.ZipCode = saved.ZipCode;
                    form.Street = saved.Street;
                    form.Number = saved.Number;
                    form.Complement = saved.Complement;
                    form.District = saved.District;
                    form.City = saved.City;
                    form.State = saved.State;
                }
            }
        }

        ViewData["ShippingOptions"] = await QuoteAsync(cart, form.ZipCode, cancellationToken);

        return View(form);
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

        IReadOnlyList<ShippingOption> shippingOptions;

        try
        {
            shippingOptions = await QuoteAsync(cart, form.ZipCode, cancellationToken);
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(form);
        }

        ViewData["ShippingOptions"] = shippingOptions;

        var shipping = shippingOptions.FirstOrDefault(option => option.Code == form.ShippingCode);

        if (shipping is null)
        {
            ModelState.AddModelError(string.Empty, "Escolha uma opção de entrega válida.");
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
                // O Id do Identity é string; o pedido usa Guid.
                var identityUser = await userManager.GetUserAsync(User);
                if (Guid.TryParse(identityUser?.Id, out var parsedCustomerId))
                {
                    customerId = parsedCustomerId;
                }
            }

            var order = await orderService.PlaceOrderAsync(
                cart,
                customerId,
                customerId is null ? form.GuestEmail : null,
                address,
                shipping,
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

    private async Task<IReadOnlyList<ShippingOption>> QuoteAsync(
        Cart cart,
        string? zipCode,
        CancellationToken cancellationToken)
    {
        var weight = 0;

        foreach (var item in cart.Items)
        {
            var variant = await products.GetVariantSnapshotAsync(item.ProductVariantId, cancellationToken);
            weight += (variant?.WeightGrams ?? 0) * item.Quantity;
        }

        return await shippingProvider.QuoteAsync(
            new ShippingQuoteRequest(zipCode ?? string.Empty, weight, cart.Subtotal),
            cancellationToken);
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
