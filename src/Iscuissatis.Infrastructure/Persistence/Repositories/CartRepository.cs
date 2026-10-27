using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Repositories;

public sealed class CartRepository(AppDbContext dbContext) : ICartRepository
{
    public Task<Cart?> FindByAnonymousIdAsync(string anonymousId, CancellationToken cancellationToken = default)
        => dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.AnonymousId == anonymousId, cancellationToken);

    public Task<Cart?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken);

    public void Add(Cart cart) => dbContext.Carts.Add(cart);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
