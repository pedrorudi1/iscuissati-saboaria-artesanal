using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Sales;

/// <summary>
/// Endereço salvo na conta do cliente. O primeiro vira o padrão automaticamente;
/// só um endereço por cliente pode ser padrão.
/// </summary>
public sealed class Address
{
    private Address()
    {
    }

    private Address(
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
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Label = label;
        Recipient = recipient;
        Street = street;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
        ZipCode = zipCode;
        IsDefault = false;
        CreatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    /// <summary>Apelido dado pelo cliente. Ex.: "Casa", "Trabalho".</summary>
    public string Label { get; private set; } = string.Empty;

    public string Recipient { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string? Complement { get; private set; }

    public string District { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Address Create(
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
        DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Endereço precisa pertencer a um cliente.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException("Dê um nome ao endereço (ex.: Casa).");
        }

        var shipping = ShippingAddress.Create(recipient, street, number, complement, district, city, state, zipCode);

        return new Address(
            customerId,
            label.Trim(),
            shipping.Recipient,
            shipping.Street,
            shipping.Number,
            shipping.Complement,
            shipping.District,
            shipping.City,
            shipping.State,
            shipping.ZipCode,
            now);
    }

    public void MakeDefault()
    {
        IsDefault = true;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
    }

    public ShippingAddress ToShippingAddress()
        => ShippingAddress.Create(Recipient, Street, Number, Complement, District, City, State, ZipCode);
}
