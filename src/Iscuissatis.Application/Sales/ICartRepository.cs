using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Sales;

public interface ICartRepository
{
    Task<Cart?> FindByAnonymousIdAsync(string anonymousId, CancellationToken cancellationToken = default);

    Task<Cart?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    void Add(Cart cart);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
