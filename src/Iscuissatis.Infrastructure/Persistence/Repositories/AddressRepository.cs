using Iscuissatis.Application.Sales;
using Iscuissatis.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Repositories;

public sealed class AddressRepository(AppDbContext dbContext) : IAddressRepository
{
    public async Task<IReadOnlyList<Address>> ListAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await dbContext.Addresses
            .Where(address => address.CustomerId == customerId)
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.Label)
            .ToListAsync(cancellationToken);

    public Task<Address?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Addresses.FirstOrDefaultAsync(address => address.Id == id, cancellationToken);

    public void Add(Address address) => dbContext.Addresses.Add(address);

    public void Remove(Address address) => dbContext.Addresses.Remove(address);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
