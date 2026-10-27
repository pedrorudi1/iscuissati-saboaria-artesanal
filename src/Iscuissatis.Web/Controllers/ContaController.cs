using Iscuissatis.Application.Identity;
using Iscuissatis.Application.Sales;
using Iscuissatis.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

[Authorize(Roles = Roles.Cliente)]
public class ContaController(
    UserManager<ApplicationUser> userManager,
    IOrderRepository orders) : Controller
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

    private async Task<Guid?> GetCustomerIdAsync()
    {
        var user = await userManager.GetUserAsync(User);
        return Guid.TryParse(user?.Id, out var id) ? id : null;
    }
}
