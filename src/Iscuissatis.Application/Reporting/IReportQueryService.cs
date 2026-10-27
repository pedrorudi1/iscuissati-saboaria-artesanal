namespace Iscuissatis.Application.Reporting;

public sealed record DashboardSummary(
    decimal Revenue,
    int PaidOrders,
    decimal AverageTicket,
    int PendingPaymentOrders,
    int OrdersToday);

public sealed record TopProduct(Guid ProductVariantId, string ProductName, string VariantName, int Quantity, decimal Revenue);

public sealed record LowStockItem(Guid VariantId, string ProductName, string VariantName, string Sku, int StockQuantity);

public sealed record DailyRevenue(DateOnly Day, decimal Revenue, int Orders);

public interface IReportQueryService
{
    Task<DashboardSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopProduct>> GetTopProductsAsync(DateTimeOffset from, DateTimeOffset to, int take = 5, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(int threshold = 5, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
