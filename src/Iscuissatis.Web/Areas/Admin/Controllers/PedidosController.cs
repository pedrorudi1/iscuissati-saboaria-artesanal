using Iscuissatis.Application.Identity;
using Iscuissatis.Application.Notifications;
using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminArea)]
public class PedidosController(IOrderRepository orders, OrderService orderService, IOrderEmailNotifier orderEmails) : Controller
{
    [HttpGet("/admin/pedidos")]
    public async Task<IActionResult> Index(
        string? q = null,
        OrderStatus? status = null,
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await orders.ListAsync(q, status, pagina, 20, cancellationToken);

        ViewData["Title"] = "Pedidos";
        ViewData["Search"] = q;
        ViewData["Status"] = status;
        ViewData["Message"] = Request.Query["ok"].ToString();
        ViewData["Error"] = Request.Query["erro"].ToString();

        return View(result);
    }

    [HttpGet("/admin/pedidos/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(id, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Pedido {order.Number}";
        ViewData["Message"] = Request.Query["ok"].ToString();
        ViewData["Error"] = Request.Query["erro"].ToString();

        return View(order);
    }

    [HttpPost("/admin/pedidos/{id:guid}/cancelar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderService.CancelAsync(id, cancellationToken);
            await orderEmails.NotifyAsync(order, OrderEmailKind.Canceled, cancellationToken);

            return RedirectToAction(nameof(Details), new { id, ok = "Pedido cancelado e estoque devolvido." });
        }
        catch (DomainException exception)
        {
            return RedirectToAction(nameof(Details), new { id, erro = exception.Message });
        }
    }
}
