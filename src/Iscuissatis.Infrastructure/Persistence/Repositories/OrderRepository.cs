using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task<string?> CreateWithReservationAsync(
        Order order,
        IReadOnlyList<StockRequest> requests,
        IReadOnlyList<StockReservation> reservations,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var request in requests)
        {
            // UPDATE condicional: só desconta se ainda houver estoque suficiente.
            // É o que impede duas compras simultâneas da última unidade.
            var affected = await dbContext.ProductVariants
                .Where(variant => variant.Id == request.VariantId
                                  && variant.IsActive
                                  && variant.StockQuantity >= request.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        variant => variant.StockQuantity,
                        variant => variant.StockQuantity - request.Quantity),
                    cancellationToken);

            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return $"Estoque insuficiente para {request.ProductName}. Atualize a sacola e tente de novo.";
            }
        }

        dbContext.Orders.Add(order);
        dbContext.StockReservations.AddRange(reservations);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return null;
    }

    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task<Order?> FindByNumberAsync(string number, CancellationToken cancellationToken = default)
        => dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Number == number, cancellationToken);

    public async Task ReleaseReservationsAsync(
        Guid orderId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var reservations = await dbContext.StockReservations
            .Where(reservation => reservation.OrderId == orderId && reservation.ReleasedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            await dbContext.ProductVariants
                .Where(variant => variant.Id == reservation.ProductVariantId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        variant => variant.StockQuantity,
                        variant => variant.StockQuantity + reservation.Quantity),
                    cancellationToken);

            reservation.Release(now);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
