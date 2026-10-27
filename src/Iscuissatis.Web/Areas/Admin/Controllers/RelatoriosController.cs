using System.Globalization;
using System.Text;
using Iscuissatis.Application.Identity;
using Iscuissatis.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminArea)]
public class RelatoriosController(IReportQueryService reports) : Controller
{
    [HttpGet("/admin/relatorios")]
    public async Task<IActionResult> Index(DateTime? de = null, DateTime? ate = null, CancellationToken cancellationToken = default)
    {
        var (from, to) = Range(de, ate);

        ViewData["Title"] = "Relatórios";
        ViewData["From"] = from.ToLocalTime().ToString("yyyy-MM-dd");
        ViewData["To"] = to.AddDays(-1).ToLocalTime().ToString("yyyy-MM-dd");

        var model = new ReportsViewModel
        {
            Summary = await reports.GetSummaryAsync(from, to, cancellationToken),
            Daily = await reports.GetDailyRevenueAsync(from, to, cancellationToken),
            TopProducts = await reports.GetTopProductsAsync(from, to, 10, cancellationToken),
        };

        return View(model);
    }

    [HttpGet("/admin/relatorios/vendas.csv")]
    public async Task<IActionResult> ExportCsv(DateTime? de = null, DateTime? ate = null, CancellationToken cancellationToken = default)
    {
        var (from, to) = Range(de, ate);
        var daily = await reports.GetDailyRevenueAsync(from, to, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("data;pedidos;faturamento");

        foreach (var row in daily)
        {
            csv.AppendLine(string.Join(';',
                row.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.Orders.ToString(CultureInfo.InvariantCulture),
                row.Revenue.ToString("F2", CultureInfo.InvariantCulture)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

        return File(bytes, "text/csv", $"vendas-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private static (DateTimeOffset From, DateTimeOffset To) Range(DateTime? de, DateTime? ate)
    {
        var start = de?.Date ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = (ate?.Date ?? DateTime.UtcNow.Date).AddDays(1);

        return (new DateTimeOffset(start, TimeSpan.Zero), new DateTimeOffset(end, TimeSpan.Zero));
    }
}

public class ReportsViewModel
{
    public DashboardSummary Summary { get; set; } = new(0, 0, 0, 0, 0);

    public IReadOnlyList<DailyRevenue> Daily { get; set; } = [];

    public IReadOnlyList<TopProduct> TopProducts { get; set; } = [];
}
