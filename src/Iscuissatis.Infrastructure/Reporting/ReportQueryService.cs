using Iscuissatis.Application.Reporting;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Reporting;

/// <summary>
/// Consultas de relatório. Só pedidos efetivamente pagos entram no faturamento:
/// pendente de pagamento é dinheiro que ainda não existe.
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
        var paid = dbContext.Orders.AsNoTracking()
            .Where(order => PaidStatuses.Contains(order.Status)
                            && order.CreatedAtUtc >= from && order.CreatedAtUtc < to);

        var revenue = await paid.SumAsync(
            order => (decimal?)(order.Subtotal + order.Shipping - order.Discount), cancellationToken) ?? 0m;

        var count = await paid.CountAsync(cancellationToken);

        var pending = await dbContext.Orders.AsNoTracking()
            .CountAsync(order => order.Status == OrderStatus.PendingPayment
                                 && order.CreatedAtUtc >= from && order.CreatedAtUtc < to, cancellationToken);

        var today = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);

        var ordersToday = await dbContext.Orders.AsNoTracking()
            .CountAsync(order => order.CreatedAtUtc >= today, cancellationToken);

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
        => await dbContext.OrderItems.AsNoTracking()
            .Join(
                dbContext.Orders.AsNoTracking().Where(order =>
                    PaidStatuses.Contains(order.Status)
                    && order.CreatedAtUtc >= from && order.CreatedAtUtc < to),
                item => item.OrderId,
                order => order.Id,
                (item, _) => item)
            .GroupBy(item => new { item.ProductVariantId, item.ProductName, item.VariantName })
            .Select(group => new TopProduct(
                group.Key.ProductVariantId,
                group.Key.ProductName,
                group.Key.VariantName,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.Quantity * item.UnitPrice)))
            .OrderByDescending(item => item.Quantity)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(
        int threshold = 5,
        CancellationToken cancellationToken = default)
        => await (from variant in dbContext.ProductVariants.AsNoTracking()
                  join product in dbContext.Products.AsNoTracking() on variant.ProductId equals product.Id
                  where variant.IsActive && variant.StockQuantity <= threshold
                  orderby variant.StockQuantity
                  select new LowStockItem(
                      variant.Id, product.Name, variant.Name, variant.Sku, variant.StockQuantity))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Orders.AsNoTracking()
            .Where(order => PaidStatuses.Contains(order.Status)
                            && order.CreatedAtUtc >= from && order.CreatedAtUtc < to)
            .Select(order => new
            {
                order.CreatedAtUtc,
                Total = order.Subtotal + order.Shipping - order.Discount,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => DateOnly.FromDateTime(row.CreatedAtUtc.UtcDateTime))
            .OrderBy(group => group.Key)
            .Select(group => new DailyRevenue(group.Key, group.Sum(row => row.Total), group.Count()))
            .ToList();
    }
}
