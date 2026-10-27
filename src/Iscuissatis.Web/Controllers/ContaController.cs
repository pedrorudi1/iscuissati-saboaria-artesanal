using Iscuissatis.Application.Identity;
using Iscuissatis.Application.Sales;
using Iscuissatis.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Iscuissatis.Web.Models;
using Iscuissatis.Domain.Common;

namespace Iscuissatis.Web.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class ContaController(
    UserManager<ApplicationUser> userManager,
    IOrderRepository orders,
    AddressService addressService) : Controller
{
    [HttpGet("/minha-conta")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var customerId = await GetCustomerIdAsync();

        if (customerId is null)
        {
            return Challenge();
        }

        var ordersOfCustomer = await orders.ListByCustomerAsync(customerId.Value, cancellationToken);

        ViewData["Title"] = "Minha conta";
        return View(ordersOfCustomer);
    }

    [HttpGet("/minha-conta/pedidos/{number}")]
    public async Task<IActionResult> Order(string number, CancellationToken cancellationToken)
    {
        var customerId = await GetCustomerIdAsync();
        var order = await orders.FindByNumberAsync(number, cancellationToken);

        // Pedido de outra pessoa responde 404: não se revela que existe.
        if (customerId is null || order is null || order.CustomerId != customerId)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Pedido {order.Number}";
        return View(order);
    }

    [HttpGet("/minha-conta/enderecos")]
    public async Task<IActionResult> Addresses(CancellationToken cancellationToken)
    {
        var customerId = await GetCustomerIdAsync();

        if (customerId is null)
        {
            return Challenge();
        }

        ViewData["Title"] = "Meus endereços";
        return View(await addressService.ListAsync(customerId.Value, cancellationToken));
    }

    [HttpPost("/minha-conta/enderecos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddress(AddressFormModel form, CancellationToken cancellationToken)
    {
        var customerId = await GetCustomerIdAsync();

        if (customerId is null)
        {
            return Challenge();
        }

        try
        {
            await addressService.CreateAsync(
                customerId.Value, form.Label, form.Recipient, form.Street, form.Number,
                form.Complement, form.District ?? string.Empty, form.City, form.State, form.ZipCode,
                cancellationToken);

            return RedirectToAction(nameof(Addresses));
        }
        catch (DomainException exception)
        {
            return RedirectToAction(nameof(Addresses), new { erro = exception.Message });
        }
    }

    [HttpPost("/minha-conta/enderecos/{id:guid}/padrao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken cancellationToken)
    {
        var customerId = await GetCustomerIdAsync();

        if (customerId is null)
        {
            return Challenge();
        }

        try
        {
            await addressService.SetDefaultAsync(customerId.Value, id, cancellationToken);
        }
        catch (DomainException exception)
        {
            return RedirectToAction(nameof(Addresses), new { erro = exception.Message });
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("/minha-conta/enderecos/{id:guid}/remover")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        var customerId = await GetCustomerIdAsync();

        if (customerId is null)
        {
            return Challenge();
        }

        try
        {
            await addressService.DeleteAsync(customerId.Value, id, cancellationToken);
        }
        catch (DomainException exception)
        {
            return RedirectToAction(nameof(Addresses), new { erro = exception.Message });
        }

        return RedirectToAction(nameof(Addresses));
    }

    private async Task<Guid?> GetCustomerIdAsync()
    {
        var user = await userManager.GetUserAsync(User);
        return Guid.TryParse(user?.Id, out var id) ? id : null;
    }
}
