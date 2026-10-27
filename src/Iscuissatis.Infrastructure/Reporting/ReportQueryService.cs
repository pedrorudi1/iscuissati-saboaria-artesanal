using Iscuissatis.Application.Reporting;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Reporting;

/// <summary>
/// Consultas de relatório. Só pedidos pagos entram no faturamento:
/// pendente de pagamento é dinheiro que ainda não existe.
/// Agregações simples são feitas em memória — o volume de uma loja pequena
/// não justifica SQL complexo e evita risco de tradução do EF.
/// </summary>
public sealed class ReportQueryService(AppDbContext dbContext) : IReportQueryService
{
    private static readonly OrderStatus[] PaidStatuses =
    [
        OrderStatus.Paid, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Delivered,
    ];

    public async Task<DashboardSummary> GetSummaryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var paidOrders = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CreatedAtUtc >= from && order.CreatedAtUtc < to)
            .Select(order => new { order.Status, order.Subtotal, order.Shipping, order.Discount, order.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        var paid = paidOrders.Where(order => PaidStatuses.Contains(order.Status)).ToList();

        var revenue = paid.Sum(order => order.Subtotal + order.Shipping - order.Discount);
        var count = paid.Count;

        var pending = paidOrders.Count(order => order.Status == OrderStatus.PendingPayment);

        var dayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var ordersToday = await dbContext.Orders
            .AsNoTracking()
            .CountAsync(order => order.CreatedAtUtc >= dayStart, cancellationToken);

        return new DashboardSummary(
            revenue,
            count,
            count == 0 ? 0m : Math.Round(revenue / count, 2, MidpointRounding.AwayFromZero),
            pending,
            ordersToday);
    }

    public async Task<IReadOnlyList<TopProduct>> GetTopProductsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int take = 5,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item => dbContext.Orders.Any(order =>
                order.Id == item.OrderId
                && order.CreatedAtUtc >= from && order.CreatedAtUtc < to
                && PaidStatuses.Contains(order.Status)))
            .Select(item => new
            {
                item.ProductVariantId,
                item.ProductName,
                item.VariantName,
                item.Quantity,
                item.UnitPrice,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new { row.ProductVariantId, row.ProductName, row.VariantName })
            .Select(group => new TopProduct(
                group.Key.ProductVariantId,
                group.Key.ProductName,
                group.Key.VariantName,
                group.Sum(row => row.Quantity),
                group.Sum(row => row.Quantity * row.UnitPrice)))
            .OrderByDescending(item => item.Quantity)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(
        int threshold = 5,
        CancellationToken cancellationToken = default)
    {
        var rows = await (from variant in dbContext.ProductVariants.AsNoTracking()
                          join product in dbContext.Products.AsNoTracking() on variant.ProductId equals product.Id
                          where variant.IsActive && variant.StockQuantity <= threshold
                          select new
                          {
                              variant.Id,
                              ProductName = product.Name,
                              variant.Name,
                              variant.Sku,
                              variant.StockQuantity,
                          })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.StockQuantity)
            .Select(row => new LowStockItem(row.Id, row.ProductName, row.Name, row.Sku, row.StockQuantity))
            .ToList();
    }

    public async Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CreatedAtUtc >= from && order.CreatedAtUtc < to)
            .Select(order => new { order.Status, order.CreatedAtUtc, order.Subtotal, order.Shipping, order.Discount })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row => PaidStatuses.Contains(row.Status))
            .GroupBy(row => DateOnly.FromDateTime(row.CreatedAtUtc.UtcDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new DailyRevenue(
                group.Key,
                group.Sum(row => row.Subtotal + row.Shipping - row.Discount),
                group.Count()))
            .ToList();
    }
}
