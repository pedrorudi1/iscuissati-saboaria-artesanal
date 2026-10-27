using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Sales;

public interface IAddressRepository
{
    Task<IReadOnlyList<Address>> ListAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<Address?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Address address);

    void Remove(Address address);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class AddressService(IAddressRepository addresses, TimeProvider clock)
{
    public Task<IReadOnlyList<Address>> ListAsync(Guid customerId, CancellationToken cancellationToken = default)
        => addresses.ListAsync(customerId, cancellationToken);

    public async Task<Address> CreateAsync(
        Guid customerId,
        string label,
        string recipient,
        string street,
        string number,
        string? complement,
        string district,
        string city,
        string state,
        string zipCode,
        CancellationToken cancellationToken = default)
    {
        var existing = await addresses.ListAsync(customerId, cancellationToken);

        var address = Address.Create(
            customerId, label, recipient, street, number, complement,
            district, city, state, zipCode, clock.GetUtcNow());

        if (existing.Count == 0)
        {
            address.MakeDefault();
        }

        addresses.Add(address);
        await addresses.SaveChangesAsync(cancellationToken);

        return address;
    }

    public async Task SetDefaultAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var all = await addresses.ListAsync(customerId, cancellationToken);

        var target = all.FirstOrDefault(item => item.Id == addressId)
            ?? throw new DomainException("Endereço não encontrado.");

        foreach (var address in all)
        {
            if (address.IsDefault && address.Id != target.Id)
            {
                address.RemoveDefault();
            }
        }

        target.MakeDefault();
        await addresses.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var all = await addresses.ListAsync(customerId, cancellationToken);

        var target = all.FirstOrDefault(item => item.Id == addressId)
            ?? throw new DomainException("Endereço não encontrado.");

        if (all.Count == 1)
        {
            throw new DomainException("Mantenha ao menos um endereço salvo. Edite-o em vez de excluir.");
        }

        if (target.IsDefault)
        {
            all.First(item => item.Id != target.Id).MakeDefault();
        }

        addresses.Remove(target);
        await addresses.SaveChangesAsync(cancellationToken);
    }
}
