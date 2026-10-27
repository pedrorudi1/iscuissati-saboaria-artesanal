using Iscuissatis.Application.Identity;
using Iscuissatis.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminArea)]
public class DashboardController(IReportQueryService reports) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        ViewData["Title"] = "Dashboard";
        ViewData["Month"] = await reports.GetSummaryAsync(monthStart, monthStart.AddMonths(1), cancellationToken);
        ViewData["Today"] = await reports.GetSummaryAsync(dayStart, dayStart.AddDays(1), cancellationToken);
        ViewData["LowStock"] = await reports.GetLowStockAsync(5, cancellationToken);
        ViewData["TopProducts"] = await reports.GetTopProductsAsync(monthStart, monthStart.AddMonths(1), 5, cancellationToken);

        return View();
    }
}
